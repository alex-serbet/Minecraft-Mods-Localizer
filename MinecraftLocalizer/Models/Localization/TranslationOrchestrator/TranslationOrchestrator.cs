using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Localization.Requests;
using MinecraftLocalizer.Models.Localization.TextProcessors;
using MinecraftLocalizer.Models.Localization.ModContext;
using MinecraftLocalizer.Models.Services.Translation;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class TranslationOrchestrator
    {
        private readonly LocalizationDocumentStore _localizationManager;
        private readonly IProgress<(int current, int total, double percentage)> _progress;
        private readonly Action<string>? _onStreamingChunkReceived;
        private readonly Action<string>? _onLogMessage;
        private readonly Action<string, IReadOnlyList<LocalizationItem>, string>? _onDocumentSnapshot;
        private readonly TranslationModeItem? _selectedMode;
        private readonly TranslationProvider _provider;
        private readonly HashSet<string> _patchouliSeededArchives = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _modContextByJarPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly TranslationGlossary _glossary = new();
        private readonly HashSet<string> _patchouliKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "name",
            "title",
            "text",
            "description"
        };

        private static readonly Dictionary<string, string[]> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".json"] = [".json"],
            [".lang"] = [".lang"],
            [".snbt"] = [".snbt"]
        };

        public static readonly Regex TranslationMarkerRegex =
            new(@"@(\d+)\s+(.+?)\s+\1@", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        public static int GetBatchSize(TranslationProvider provider)
        {
            int configuredValue = provider switch
            {
                TranslationProvider.Gpt4Free    => Properties.Settings.Default.Gpt4FreeBatchSize,
                TranslationProvider.Gemini       => Properties.Settings.Default.GeminiBatchSize,
                TranslationProvider.OpenRouter   => Properties.Settings.Default.OpenRouterBatchSize,
                _                                => Properties.Settings.Default.DeepSeekBatchSize,
            };

            return Math.Clamp(configuredValue, 1, 500);
        }
        private static readonly ConcurrentDictionary<string, ILoadSource> LoadSourceCache = new();

        public TranslationOrchestrator(
            LocalizationDocumentStore localizationManager,
            IProgress<(int current, int total, double percentage)> progress,
            Action<string>? onStreamingChunkReceived = null,
            Action<string>? onLogMessage = null,
            Action<string, IReadOnlyList<LocalizationItem>, string>? onDocumentSnapshot = null,
            TranslationModeItem? selectedMode = null,
            TranslationProvider provider = TranslationProvider.DeepSeek)
        {
            _localizationManager = localizationManager;
            _progress = progress;
            _onStreamingChunkReceived = onStreamingChunkReceived;
            _onLogMessage = onLogMessage;
            _onDocumentSnapshot = onDocumentSnapshot;
            _selectedMode = selectedMode;
            _provider = provider;
        }

        public async Task<bool> TranslateSelectedStrings(
            List<TreeNodeItem> selectedNodes,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            bool isTextBoxMode = false,
            string? currentDataGridFilePath = null,
            HashSet<string>? selectedEntryKeys = null)
        {
            if (selectedNodes.Count == 0)
                return false;

            Log($"Translation started. Mode: {modeType}. Nodes: {selectedNodes.Count}. Provider: {_provider}.");

            return isTextBoxMode
                ? await ProcessTextBoxTranslation(cancellationToken)
                : await ProcessDataGridTranslation(
                    selectedNodes,
                    modeType,
                    cancellationToken,
                    currentDataGridFilePath,
                    selectedEntryKeys);
        }

        private async Task<bool> ProcessTextBoxTranslation(CancellationToken cancellationToken)
        {
            var processor = CreateTextProcessor();
            return await processor.ProcessAsync(cancellationToken);
        }

        private RegularTextProcessor CreateTextProcessor()
        {
            return new RegularTextProcessor(_localizationManager, _progress, _onStreamingChunkReceived, _onLogMessage, _provider);
        }

        private async Task<bool> ProcessDataGridTranslation(
            List<TreeNodeItem> selectedNodes,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            var sourceLanguage = Properties.Settings.Default.SourceLanguage;
            var nodePairs = CollectTargetNodes(selectedNodes, sourceLanguage, modeType);

            if (nodePairs.Count == 0)
                return false;

            Log($"Source language: {sourceLanguage}. File pairs: {nodePairs.Count}.");

            await BuildModContextsAsync(nodePairs, modeType, cancellationToken);
            LoadGlossaryCache(nodePairs);

            int totalStrings = await CalculateTotalStringsAsync(
                nodePairs,
                modeType,
                currentDataGridFilePath,
                selectedEntryKeys);
            if (totalStrings == 0)
                return false;

            int totalTranslated = 0;
            _progress.Report((0, totalStrings, 0));
            Log($"Total strings to translate: {totalStrings}.");

            try
            {
                foreach (var (source, target) in nodePairs)
                {
                    int translatedInThisFile = await ProcessNodePair(
                        source,
                        target,
                        modeType,
                        cancellationToken,
                        totalStrings,
                        totalTranslated,
                        currentDataGridFilePath,
                        selectedEntryKeys);

                    totalTranslated += translatedInThisFile;
                }
            }
            finally
            {
                SaveGlossaryCache(nodePairs);
            }

            return true;
        }

        private void LoadGlossaryCache(List<(TreeNodeItem Source, TreeNodeItem Target)> nodePairs)
        {
            foreach (var jarPath in CollectJarPaths(nodePairs))
            {
                try
                {
                    string cachePath = ResolveModCachePath(jarPath);
                    if (!File.Exists(cachePath)) continue;

                    string json = File.ReadAllText(cachePath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("Glossary", out var glossaryEl) &&
                        glossaryEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(glossaryEl.GetRawText());
                        int before = _glossary.Count;
                        _glossary.LoadFrom(dict);
                        int loaded = _glossary.Count - before;
                        if (loaded > 0)
                            Log($"Loaded {loaded} glossary entries from {Path.GetFileName(cachePath)}.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to load glossary for {Path.GetFileName(jarPath)}: {ex.Message}");
                }
            }
        }

        private void SaveGlossaryForNode(TreeNodeItem target, TreeNodeItem source)
        {
            if (_glossary.Count == 0) return;

            var glossaryDict = _glossary.ToDictionary();
            foreach (var path in new[] { target.ModPath, source.ModPath })
            {
                if (!string.IsNullOrWhiteSpace(path) &&
                    path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(path))
                {
                    try
                    {
                        string cachePath = ResolveModCachePath(path);
                        SaveGlossaryToJsonFile(cachePath, glossaryDict);
                    }
                    catch { /* best-effort */ }
                    break;
                }
            }
        }

        private void SaveGlossaryCache(List<(TreeNodeItem Source, TreeNodeItem Target)> nodePairs)
        {
            if (_glossary.Count == 0)
                return;

            var glossaryDict = _glossary.ToDictionary();

            foreach (var jarPath in CollectJarPaths(nodePairs))
            {
                try
                {
                    string cachePath = ResolveModCachePath(jarPath);
                    SaveGlossaryToJsonFile(cachePath, glossaryDict);
                    Log($"Glossary ({glossaryDict.Count} entries) saved to {Path.GetFileName(cachePath)}.");
                }
                catch (Exception ex)
                {
                    Log($"Failed to save glossary for {Path.GetFileName(jarPath)}: {ex.Message}");
                }
            }
        }

        private static string ResolveModCachePath(string jarPath)
        {
            var metadata = ModMetadataExtractor.Extract(jarPath);
            string modId = metadata?.ModId ?? Path.GetFileNameWithoutExtension(jarPath);
            string dir = Path.Combine(AppContext.BaseDirectory, "cache", "mod-contexts");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, modId + ".json");
        }

        private static void SaveGlossaryToJsonFile(string cachePath, Dictionary<string, string> glossary)
        {
            Dictionary<string, object?>? existing = null;

            if (File.Exists(cachePath))
            {
                string oldJson = File.ReadAllText(cachePath);
                existing = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(oldJson);
            }

            existing ??= new Dictionary<string, object?>();
            existing["Glossary"] = glossary;

            string json = System.Text.Json.JsonSerializer.Serialize(existing, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(cachePath, json);
        }

        private static HashSet<string> CollectJarPaths(List<(TreeNodeItem Source, TreeNodeItem Target)> nodePairs)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (source, target) in nodePairs)
            {
                foreach (var path in new[] { target.ModPath, source.ModPath })
                {
                    if (!string.IsNullOrWhiteSpace(path) &&
                        path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(path))
                    {
                        paths.Add(path);
                    }
                }
            }
            return paths;
        }

        private async Task BuildModContextsAsync(
            List<(TreeNodeItem Source, TreeNodeItem Target)> nodePairs,
            TranslationModeType modeType,
            CancellationToken cancellationToken)
        {
            if (modeType != TranslationModeType.Mods && modeType != TranslationModeType.Patchouli)
                return;

            var jarPaths = CollectJarPaths(nodePairs);
            if (jarPaths.Count == 0)
                return;

            bool enableHttpFetch = Properties.Settings.Default.EnableModContextFetch;
            var service = new ModContextService(enableHttpFetch, _onLogMessage);

            Log($"Collecting mod context. Mods: {jarPaths.Count}. HTTP fetch: {enableHttpFetch}.");
            foreach (var jarPath in jarPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    string ctx = await service.GetContextForModAsync(jarPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(ctx))
                        _modContextByJarPath[jarPath] = ctx;
                }
                catch (Exception ex)
                {
                    Log($"Mod context build failed for {Path.GetFileName(jarPath)}: {ex.Message}");
                }
            }
        }

        private string? ResolveModContext(TreeNodeItem target, TreeNodeItem source)
        {
            foreach (var path in new[] { target.ModPath, source.ModPath })
            {
                if (!string.IsNullOrWhiteSpace(path) && _modContextByJarPath.TryGetValue(path, out var ctx))
                    return ctx;
            }
            return null;
        }

        private async Task<int> ProcessNodePair(
            TreeNodeItem source,
            TreeNodeItem target,
            TranslationModeType modeType,
            CancellationToken cancellationToken,
            int totalStrings,
            int totalTranslatedSoFar,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            await UpdateUIAsync(() =>
            {
                target.IsTranslating = true;
                ExpandTranslationBranch(source, target.FilePath);
            });

            await LoadStringsAsync(source, target);
            Log($"Loaded strings. Target: {target.FilePath ?? target.FileName}. Total in file: {_localizationManager.LocalizationStrings.Count}.");

            if (modeType == TranslationModeType.Patchouli)
                ApplyPatchouliSelectionByJsonReference();

            ApplySelectionForCurrentFile(target.FilePath, currentDataGridFilePath, selectedEntryKeys);
            bool allowSelectAll = selectedEntryKeys == null || selectedEntryKeys.Count == 0;
            EnsureSelectionExistsOrSelectAll(modeType, allowSelectAll);

            var selectedStrings = _localizationManager.LocalizationStrings
                .Where(e => e.IsSelected)
                .ToList();

            if (selectedStrings.Count == 0 && !allowSelectAll)
            {
                Log($"Skipped file (no selected strings). Target: {target.FilePath ?? target.FileName}.");
                target.IsTranslating = false;
                return 0;
            }

            int translatedCount = 0;
            string? modContext = ResolveModContext(target, source);
            Log($"Translating file: {target.FilePath ?? target.FileName}. Selected strings: {selectedStrings.Count}. Batch size: {GetBatchSize(_provider)}. Mod context: {(modContext != null ? "yes" : "no")}.");

            if (modeType == TranslationModeType.Patchouli)
            {
                TrySeedPatchouliArchive(source);
            }
            else
            {
                await SaveTranslations([target], modeType);
                Log($"Pre-saved file structure for: {target.FilePath ?? target.FileName}.");
                EmitDocumentSnapshot(target.FilePath);
            }

            while (selectedStrings.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int batchSize = GetBatchSize(_provider);
                var batch = selectedStrings.Take(batchSize).ToList();
                Log($"Batch start. Size: {batch.Count}. Remaining in file: {selectedStrings.Count}.");
                translatedCount += await ProcessTranslationBatch(batch, cancellationToken, modContext);

                int currentTotalTranslated = totalTranslatedSoFar + translatedCount;
                _progress.Report((currentTotalTranslated, totalStrings,
                    (double)currentTotalTranslated / totalStrings * 100));

                // Save glossary after every batch so it's available for the next batch and survives crashes.
                SaveGlossaryForNode(target, source);

                if (Properties.Settings.Default.AutoSaveAfterBatch)
                {
                    await SaveTranslations([target], modeType);
                    Log($"Auto-saved file after batch: {target.FilePath ?? target.FileName}.");
                    EmitDocumentSnapshot(target.FilePath);
                }

                selectedStrings = _localizationManager.LocalizationStrings
                    .Where(e => e.IsSelected)
                    .ToList();
            }

            await SaveTranslations([target], modeType);
            Log($"Saved translations for file: {target.FilePath ?? target.FileName}. Translated strings: {translatedCount}.");
            EmitDocumentSnapshot(target.FilePath);
            await UpdateUIAsync(() =>
            {
                target.IsTranslating = false;
            });

            return translatedCount;
        }

        private void EmitDocumentSnapshot(string? filePath)
        {
            if (_onDocumentSnapshot == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            _onDocumentSnapshot(filePath, _localizationManager.LocalizationStrings, _localizationManager.RawContent);
        }

        private async Task<int> ProcessTranslationBatch(List<LocalizationItem> batch, CancellationToken cancellationToken, string? modContext = null)
        {
            var markedTexts = batch.Select((item, i) => ($"@{i} {item.OriginalString} {i}@", i)).ToList();
            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => batch[pair.i]);

            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));

            // Pull only glossary entries whose source actually appears in THIS batch — keeps the
            // system prompt small and relevant. Pairs collected from previous batches drive
            // cross-batch terminology consistency.
            string glossaryBlock = _glossary.BuildPromptBlockForBatch(batch.Select(b => b.OriginalString ?? string.Empty));
            string? systemContext = ComposeSystemContext(modContext, glossaryBlock);

            string translatedText;
            using var translationRequest = new TranslationAiRequest(_provider, _onLogMessage, systemContext);
            while (true)
            {
                translatedText = await translationRequest.TranslateTextWithStreamingUIAsync(
                    combinedText,
                    cancellationToken,
                    chunk => _onStreamingChunkReceived?.Invoke(chunk));

                if (!string.IsNullOrWhiteSpace(translatedText))
                    break;

                // Don't skip the batch; wait and try again until we get a response.
                await Task.Delay(1000, cancellationToken);
            }

            var (translatedCount, _) = await ProcessTranslationResults(
                translatedText,
                indexMap,
                clearSelectionOnNoMatch: false);

            Log($"Batch result. Translated: {translatedCount}/{indexMap.Count}. Glossary size: {_glossary.Count}.");
            return translatedCount;
        }

        /// <summary>
        /// Concatenates the per-mod context block and the per-batch glossary block.
        /// Either piece may be empty; if both are empty, returns null so the request stays clean.
        /// </summary>
        private static string? ComposeSystemContext(string? modContext, string? glossaryBlock)
        {
            bool hasMod = !string.IsNullOrWhiteSpace(modContext);
            bool hasGlossary = !string.IsNullOrWhiteSpace(glossaryBlock);
            if (!hasMod && !hasGlossary) return null;
            if (hasMod && hasGlossary) return modContext + "\n\n" + glossaryBlock;
            return hasMod ? modContext : glossaryBlock;
        }

        private async Task<(int translatedCount, bool anyMatchFound)> ProcessTranslationResults(
            string translatedText,
            Dictionary<int, LocalizationItem> indexMap,
            bool clearSelectionOnNoMatch)
        {
            int translatedCount = 0;
            bool anyMatchFound = false;
            int invalidCount = 0;

            foreach (var kvp in indexMap)
            {
                var entry = kvp.Value;
                if (string.IsNullOrWhiteSpace(entry.OriginalString))
                {
                    await UpdateUIAsync(() =>
                    {
                        entry.TranslatedString = string.Empty;
                        entry.IsSelected = false;
                    });
                    translatedCount++;
                }
            }

            foreach (Match match in TranslationMarkerRegex.Matches(translatedText))
            {
                if (!int.TryParse(match.Groups[1].Value, out int index) ||
                    !indexMap.TryGetValue(index, out var entry))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.OriginalString))
                    continue;

                string translatedContent = match.Groups[2].Value.Trim();
                bool isValid = IsValidTranslation(entry.OriginalString ?? string.Empty, translatedContent);

                await UpdateUIAsync(() =>
                {
                    entry.TranslatedString = isValid ? translatedContent : string.Empty;
                    // Keep invalid items selected so they can be retried in the next pass.
                    entry.IsSelected = !isValid;
                });

                if (isValid)
                {
                    translatedCount++;
                    // Feed the glossary so subsequent batches stay consistent with this translation.
                    // TranslationGlossary.IsAcceptable filters out long sentences automatically.
                    _glossary.TryAdd(entry.OriginalString, translatedContent);
                }
                else
                    invalidCount++;
                anyMatchFound = true;
            }

            if (!anyMatchFound)
            {
                if (clearSelectionOnNoMatch)
                {
                    await UpdateUIAsync(() =>
                    {
                        foreach (var item in indexMap.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(item.OriginalString))
                                item.IsSelected = false;
                        }
                    });
                }
            }

            Log($"Batch parse. Valid: {translatedCount}, Invalid: {invalidCount}");
            return (translatedCount, anyMatchFound);
        }

        private void Log(string message)
        {
            _onLogMessage?.Invoke(message);
        }

        private void TrySeedPatchouliArchive(TreeNodeItem node)
        {
            if (string.IsNullOrWhiteSpace(node.ModPath))
                return;

            string seedKey = $"{node.ModPath}|{node.FilePath}";
            if (!_patchouliSeededArchives.Add(seedKey))
                return;

            LocalizationArchiveWriter.SeedPatchouliArchive(node);
            Log($"Pre-saved Patchouli structure for: {node.ModPath}.");
        }
    }
}



