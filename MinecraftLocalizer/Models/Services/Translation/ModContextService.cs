using MinecraftLocalizer.Models.Ai.Gemini;
using MinecraftLocalizer.Models.Localization.ModContext;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Services.Translation
{
    /// <summary>
    /// Builds and caches per-mod context blocks that are injected into translation prompts so the
    /// LLM keeps terminology consistent across batches. Cache lives next to the .exe under
    /// <c>cache/mod-contexts/&lt;modId&gt;.json</c>. Invalidation is based on a source signature
    /// (file size + LastWriteTime) embedded in the cached JSON.
    /// </summary>
    public sealed class ModContextService
    {
        private const string CacheFolderName = "cache";
        private const string CacheSubFolder = "mod-contexts";
        private const int HttpTimeoutSeconds = 8;
        private const string ModrinthProjectEndpoint = "https://api.modrinth.com/v2/project/";
        private const string ModrinthProjectUrlBase = "https://modrinth.com/mod/";
        private const string UserAgent = "MinecraftLocalizer/1.0 (+mod-context-fetch)";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly Regex InvalidFileNameCharsRegex = new(@"[^A-Za-z0-9._-]+", RegexOptions.Compiled);

        private readonly ConcurrentDictionary<string, string> _inMemory = new(System.StringComparer.OrdinalIgnoreCase);
        private readonly bool _enableHttpFetch;
        private readonly bool _enableSearchEnrichment;
        private readonly string? _geminiApiKey;
        private readonly Action<string>? _onLog;

        public ModContextService(bool enableHttpFetch, Action<string>? onLog = null)
        {
            _enableHttpFetch = enableHttpFetch;

            string contextKey = Properties.Settings.Default.ModContextApiKey;
            if (string.IsNullOrWhiteSpace(contextKey))
                contextKey = Properties.Settings.Default.GeminiApiKey;

            _enableSearchEnrichment = Properties.Settings.Default.EnableSearchContextEnrichment
                && !string.IsNullOrWhiteSpace(contextKey);
            _geminiApiKey = contextKey;
            _onLog = onLog;
        }

        /// <summary>
        /// Returns context text for one mod (empty string if nothing useful was found).
        /// Reads/writes a persistent file cache; falls back to a freshly built context on cache miss.
        /// </summary>
        public async Task<string> GetContextForModAsync(string modJarPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(modJarPath) || !File.Exists(modJarPath))
                return string.Empty;

            if (_inMemory.TryGetValue(modJarPath, out var cached))
                return cached;

            var metadata = ModMetadataExtractor.Extract(modJarPath);
            string signature = BuildSourceSignature(modJarPath);
            string cachePath = GetCachePath(metadata?.ModId, modJarPath);

            // Reuse cache only if the source signature matches (mod file unchanged since last build)
            // AND search enrichment was successful (or not requested).
            CachedContext? fromDisk = TryReadFromDisk(cachePath);
            if (fromDisk != null && string.Equals(fromDisk.SourceSignature, signature, StringComparison.Ordinal))
            {
                // If search is enabled but cache wasn't enriched, rebuild to retry search.
                if (_enableSearchEnrichment && !fromDisk.SearchEnriched)
                {
                    // Fall through to rebuild.
                }
                else
                {
                    _inMemory[modJarPath] = fromDisk.ContextText ?? string.Empty;
                    return fromDisk.ContextText ?? string.Empty;
                }
            }

            if (metadata == null || metadata.IsEmpty)
            {
                _inMemory[modJarPath] = string.Empty;
                TryWriteToDisk(cachePath, new CachedContext
                {
                    SourceSignature = signature,
                    ContextText = string.Empty,
                });
                return string.Empty;
            }

            ModrinthInfo? modrinth = null;
            if (_enableHttpFetch && !string.IsNullOrWhiteSpace(metadata.ModId))
            {
                modrinth = await TryFetchModrinthAsync(metadata.ModId!, cancellationToken).ConfigureAwait(false);
            }

            string contextText;
            bool searchSucceeded = false;

            // Enrich with Gemini Search: ask the model to summarize the mod from the internet.
            // If enrichment succeeds, use only it (it already contains all needed context).
            if (_enableSearchEnrichment && !string.IsNullOrWhiteSpace(metadata.ModId))
            {
                string? searchSummary = await EnrichWithGeminiSearchAsync(metadata, modrinth, cancellationToken);
                if (!string.IsNullOrWhiteSpace(searchSummary))
                {
                    contextText = searchSummary!;
                    searchSucceeded = true;
                }
                else
                {
                    contextText = BuildContextText(metadata, modrinth);
                }
            }
            else
            {
                contextText = BuildContextText(metadata, modrinth);
            }

            _inMemory[modJarPath] = contextText;
            TryWriteToDisk(cachePath, new CachedContext
            {
                ModId = metadata.ModId,
                Version = metadata.Version,
                ModrinthUrl = modrinth?.Url,
                SourceSignature = signature,
                ContextText = contextText,
                SearchEnriched = searchSucceeded,
            });

            string source = searchSucceeded ? "Google Search" : (modrinth != null ? "manifest + Modrinth" : "manifest");
            if (_enableSearchEnrichment && !searchSucceeded)
                source += " (search failed, will retry next run)";
            _onLog?.Invoke($"Mod context built. Mod: {metadata.ModId ?? Path.GetFileName(modJarPath)}. Source: {source}.");
            return contextText;
        }

        // ---- Context text -------------------------------------------------------

        /// <summary>
        /// Builds the MOD CONTEXT block injected into the system prompt.
        /// Intentionally minimal: mod id, version, Modrinth URL (only if the project exists on Modrinth).
        /// </summary>
        private static string BuildContextText(ModMetadata metadata, ModrinthInfo? modrinth)
        {
            var sb = new StringBuilder();
            sb.AppendLine("MOD CONTEXT (use it to keep terminology consistent across batches):");

            if (!string.IsNullOrWhiteSpace(metadata.ModId))
                sb.AppendLine($"- Mod id: {metadata.ModId}");
            if (!string.IsNullOrWhiteSpace(metadata.Version))
                sb.AppendLine($"- Version: {metadata.Version}");
            if (!string.IsNullOrWhiteSpace(modrinth?.Url))
                sb.AppendLine($"- Modrinth: {modrinth!.Url}");

            return sb.ToString().TrimEnd();
        }

        // ---- Optional Modrinth fetch -------------------------------------------

        /// <summary>
        /// Queries <c>https://api.modrinth.com/v2/project/{modId}</c> and returns the canonical
        /// Modrinth URL built from the <c>slug</c>. Returns null for 404 or any error so the
        /// caller silently falls back to manifest-only context.
        /// </summary>
        private async Task<ModrinthInfo?> TryFetchModrinthAsync(string modId, CancellationToken cancellationToken)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

                string url = ModrinthProjectEndpoint + Uri.EscapeDataString(modId);
                using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                string? slug = doc.RootElement.TryGetProperty("slug", out var slugEl) && slugEl.ValueKind == JsonValueKind.String
                    ? slugEl.GetString()
                    : null;

                string? projectUrl = !string.IsNullOrWhiteSpace(slug)
                    ? ModrinthProjectUrlBase + slug
                    : null;

                return projectUrl != null ? new ModrinthInfo(projectUrl) : null;
            }
            catch
            {
                return null;
            }
        }

        private sealed record ModrinthInfo(string Url);

        // ---- Gemini Search enrichment -------------------------------------------

        /// <summary>
        /// Uses Gemini with Google Search to gather a brief summary about the mod from the internet.
        /// This runs once per mod (on cache miss) and the result is cached.
        /// </summary>
        private async Task<string?> EnrichWithGeminiSearchAsync(
            ModMetadata metadata, ModrinthInfo? modrinth, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_geminiApiKey))
                return null;

            try
            {
                _onLog?.Invoke($"Searching internet for mod context: {metadata.ModId}...");

                using var client = new GeminiClient(_geminiApiKey!);
                string model = await ResolveSearchModelAsync(_geminiApiKey!, cancellationToken);

                string promptTemplate = Properties.Settings.Default.ModContextSearchPrompt;
                if (string.IsNullOrWhiteSpace(promptTemplate))
                    promptTemplate = "Search the internet for the Minecraft mod \"{modId}\". Provide a brief summary describing what this mod does, its main features, key terminology (block names, item names, entity names, mechanic names). Output ONLY the summary, no markdown.";

                string prompt = promptTemplate.Replace("{modId}", metadata.ModId ?? "");

                string result = await client.StreamGenerateAsync(
                    systemPrompt: "You are a research assistant. Summarize factual information about Minecraft mods.",
                    userText: prompt,
                    model: model,
                    temperature: 0.2,
                    isSearchingEnabled: true,
                    isThinkingEnabled: false,
                    cancellationToken: cancellationToken,
                    onChunkReceived: null).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    _onLog?.Invoke($"Search enrichment complete for {metadata.ModId} ({result.Length} chars).");
                    return $"MOD DESCRIPTION (from internet):\n{result.Trim()}";
                }
            }
            catch (Exception ex)
            {
                _onLog?.Invoke($"Search enrichment failed for {metadata.ModId}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Picks the first available Gemma model for search enrichment.
        /// Falls back to gemini-2.5-flash if none found.
        /// </summary>
        private static async Task<string> ResolveSearchModelAsync(string apiKey, CancellationToken ct)
        {
            try
            {
                var models = await MinecraftLocalizer.Models.Ai.Gemini.GeminiModelsApi.ListModelsAsync(apiKey, ct);
                var gemma = models.FirstOrDefault(m => m.Contains("gemma", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(gemma))
                    return gemma;
            }
            catch { /* best-effort */ }

            return "gemini-flash-latest";
        }

        // ---- Cache I/O ----------------------------------------------------------

        /// <summary>Signature stored inside the cache file to detect mod-file changes.</summary>
        private static string BuildSourceSignature(string modJarPath)
        {
            var info = new FileInfo(modJarPath);
            return $"{info.Length}|{info.LastWriteTimeUtc.Ticks}";
        }

        /// <summary>
        /// Cache file name = sanitized modId when available, otherwise the .jar file name (without extension).
        /// Falls back to a generic name only if both are empty.
        /// </summary>
        private static string GetCachePath(string? modId, string modJarPath)
        {
            string baseDir = AppContext.BaseDirectory;
            string dir = Path.Combine(baseDir, CacheFolderName, CacheSubFolder);
            Directory.CreateDirectory(dir);

            string fileName = SanitizeFileName(modId)
                              ?? SanitizeFileName(Path.GetFileNameWithoutExtension(modJarPath))
                              ?? "unknown";
            return Path.Combine(dir, fileName + ".json");
        }

        private static string? SanitizeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string cleaned = InvalidFileNameCharsRegex.Replace(value.Trim(), "_").Trim('_', '.');
            if (cleaned.Length > 80) cleaned = cleaned[..80];
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        private static CachedContext? TryReadFromDisk(string cachePath)
        {
            try
            {
                if (!File.Exists(cachePath)) return null;
                string json = File.ReadAllText(cachePath);
                return JsonSerializer.Deserialize<CachedContext>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void TryWriteToDisk(string cachePath, CachedContext data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(cachePath, json);
            }
            catch
            {
                // Cache is best-effort; failures must not break translation.
            }
        }

        private sealed class CachedContext
        {
            public string? ModId { get; set; }
            public string? Version { get; set; }
            public string? ModrinthUrl { get; set; }
            /// <summary>"&lt;size&gt;|&lt;ticks&gt;" of the source .jar at build time. Used for invalidation.</summary>
            public string? SourceSignature { get; set; }
            public string? ContextText { get; set; }
            /// <summary>Whether the context was enriched via Google Search.</summary>
            public bool SearchEnriched { get; set; }
            /// <summary>Glossary: source term -> translated term.</summary>
            public Dictionary<string, string>? Glossary { get; set; }
        }
    }
}
