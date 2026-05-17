using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization.ModContext
{
    /// <summary>
    /// Reads a mod's .jar without extracting and pulls the basic metadata we need for prompt context.
    /// Forge / NeoForge -> META-INF/mods.toml (regex over a few fields, no full TOML parser).
    /// Fabric / Quilt   -> fabric.mod.json / quilt.mod.json (System.Text.Json).
    /// </summary>
    public static class ModMetadataExtractor
    {
        private const string ForgeManifestEntry = "META-INF/mods.toml";
        private const string NeoForgeManifestEntry = "META-INF/neoforge.mods.toml";
        private const string FabricManifestEntry = "fabric.mod.json";
        private const string QuiltManifestEntry = "quilt.mod.json";

        // mods.toml is TOML, but we only need 4 string fields. Regexes are good enough and dependency-free.
        // Matches: key = "value" / key = 'value' / key = """multi"""
        private static readonly Regex TomlStringField = new(
            @"^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?:""""""(?<v3>(?:[^""\\]|\\.|""(?!""""))*?)""""""|""(?<v1>(?:[^""\\]|\\.)*)""|'(?<v2>[^']*)')",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public static ModMetadata? Extract(string modJarPath)
        {
            if (string.IsNullOrWhiteSpace(modJarPath) || !File.Exists(modJarPath))
                return null;

            try
            {
                using var archive = ZipFile.OpenRead(modJarPath);

                var neoforge = archive.GetEntry(NeoForgeManifestEntry);
                if (neoforge != null)
                    return ParseModsToml(ReadEntryText(neoforge));

                var forge = archive.GetEntry(ForgeManifestEntry);
                if (forge != null)
                    return ParseModsToml(ReadEntryText(forge));

                var fabric = archive.GetEntry(FabricManifestEntry);
                if (fabric != null)
                    return ParseFabricJson(ReadEntryText(fabric), isQuilt: false);

                var quilt = archive.GetEntry(QuiltManifestEntry);
                if (quilt != null)
                    return ParseFabricJson(ReadEntryText(quilt), isQuilt: true);
            }
            catch
            {
                // Malformed jar / unreadable manifest — treat as "no context available".
            }

            return null;
        }

        private static string ReadEntryText(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // ---- Forge / NeoForge ---------------------------------------------------

        private static ModMetadata ParseModsToml(string tomlText)
        {
            // mods.toml: top-level may have displayURL/license/etc.,
            // and [[mods]] block has modId/displayName/description/authors.
            var fields = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (Match m in TomlStringField.Matches(tomlText))
            {
                string key = m.Groups["key"].Value;
                string value = m.Groups["v3"].Success ? m.Groups["v3"].Value
                             : m.Groups["v1"].Success ? m.Groups["v1"].Value
                             : m.Groups["v2"].Value;

                // Keep the first occurrence — the modId/displayName from the first [[mods]] block.
                if (!fields.ContainsKey(key))
                    fields[key] = value;
            }

            return new ModMetadata
            {
                ModId = TryGet(fields, "modId"),
                Version = TryGet(fields, "version"),
            };
        }

        private static string? TryGet(Dictionary<string, string> fields, string key)
        {
            return fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        // ---- Fabric / Quilt -----------------------------------------------------

        private static ModMetadata ParseFabricJson(string json, bool isQuilt)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Quilt wraps everything under "quilt_loader" -> optional "metadata".
                JsonElement modRoot = root;
                if (isQuilt && root.TryGetProperty("quilt_loader", out var quiltLoader))
                {
                    modRoot = quiltLoader;
                    if (modRoot.TryGetProperty("metadata", out var metadata))
                        modRoot = metadata;
                }

                return new ModMetadata
                {
                    ModId = GetString(modRoot, "id") ?? GetString(root, "id"),
                    Version = GetString(modRoot, "version") ?? GetString(root, "version"),
                };
            }
            catch
            {
                return new ModMetadata();
            }
        }

        private static string? GetString(JsonElement element, string property)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;
            if (!element.TryGetProperty(property, out var value))
                return null;
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
    }
}
