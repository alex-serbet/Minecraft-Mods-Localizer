using MinecraftLocalizer.Converters;
using MinecraftLocalizer.Models.Services;
using MinecraftLocalizer.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;


namespace MinecraftLocalizer.Models.Localization
{
    public partial class LocalizationSaveManager()
    {
        private static readonly Settings Settings = Settings.Default;

        [GeneratedRegex(@"^[a-z]{2}_[a-z]{2}$", RegexOptions.IgnoreCase)]
        private static partial Regex LocaleRegex();


        public static void SaveTranslation(List<TreeNodeItem> checkedNodes, ObservableCollection<LocalizationItem> localizationStrings, TranslationModeType modeType)
        {
            if (checkedNodes.Count == 0)
            {
                throw new InvalidOperationException(Resources.NoCheckedFilesSavingMessage);
            }

            try
            {
                string zipModPath = Path.Combine(Settings.DirectoryPath, "resourcepacks", "MinecraftLocalizer.zip");

                using FileStream zipStream = new(zipModPath, FileMode.OpenOrCreate);
                using ZipArchive archive = new(zipStream, ZipArchiveMode.Update, true);

                if (!archive.Entries.Any(e => e.FullName == "pack.mcmeta"))
                {
                    AddPackMeta(archive);
                    AddPackImage(archive);
                }

                foreach (TreeNodeItem node in checkedNodes)
                {
                    SaveLocalizationForNode(node, localizationStrings, archive, modeType);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Failed to save translation. \n{ex.Message}");
            }
        }

        private static void AddPackMeta(ZipArchive archive)
        {
            var entry = archive.CreateEntry("pack.mcmeta");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);

            string description = $"§eLocalization for [{Settings.TargetLanguage}]\\n§bMade by alex-serbet";

            string packMeta = $$"""
                {
                    "pack": {
                        "pack_format": 8,
                        "supported_formats": [8, 9999],
                        "description": {{JsonConvert.ToString(description)}}
                    }
                }
                """;

            writer.Write(packMeta);
        }

        private static void AddPackImage(ZipArchive archive)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MinecraftLocalizer.Assets.pack.png";

            using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);

            if (resourceStream != null)
            {
                ZipArchiveEntry packImageEntry = archive.CreateEntry("pack.png");

                using Stream packImageStream = packImageEntry.Open();
                resourceStream.CopyTo(packImageStream);
            }
        }

        private static void SaveLocalizationForNode(TreeNodeItem node, ObservableCollection<LocalizationItem> localizationStrings, ZipArchive archive, TranslationModeType modeType)
        {
            if (localizationStrings.Count == 0 || string.IsNullOrEmpty(node?.FileName)) return;

            var targetLanguage = Settings.TargetLanguage;
          
            switch (modeType)
            {
                case TranslationModeType.Quests:
                    SaveQuestLocalization(node, localizationStrings);
                    break;

                case TranslationModeType.BetterQuesting:
                    SaveBetterQuestingLocalization(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.Patchouli:
                    SavePatchouliLocalization(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.Mods:
                    SaveModsLocalization(node, localizationStrings, archive, targetLanguage);
                    break;
            }
        }


        private static void SaveQuestLocalization(TreeNodeItem node, IEnumerable<LocalizationItem> strings)
        {
            var fileFormat = Path.GetExtension(node.FileName)?.TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(fileFormat))
            {
                fileFormat = Path.GetExtension(node.ChildrenNodes.FirstOrDefault()?.FileName)?.TrimStart('.') ?? "json";
            }

            ArgumentNullException.ThrowIfNull(node);

            var basePath = fileFormat == "json"
                ? Path.Combine(Settings.DirectoryPath, "kubejs", "assets", "kubejs", "lang")
                : Path.Combine(Settings.DirectoryPath, "config", "ftbquests", "quests", "lang");

            Directory.CreateDirectory(basePath);
            var outputFile = Path.Combine(basePath, $"{Settings.TargetLanguage}.{fileFormat}");

            using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false));
            WriteLocalizationContent(writer, strings, fileFormat);
        }

        private static void SaveBetterQuestingLocalization(TreeNodeItem node, IEnumerable<LocalizationItem> strings, ZipArchive archive, string targetLanguage)
        {
            if (node.FileName == null) return;

            string path = Path.Combine("assets", "betterquesting", "lang");
            string fileExtension = Path.GetExtension(node.FileName).ToLowerInvariant();
            string updatedPath;
            string fileFormat;

            var langRegex = LocaleRegex();
            TreeNodeItem? childNode = node.ChildrenNodes.FirstOrDefault(n => n.FileName != null && langRegex.IsMatch(Path.GetFileNameWithoutExtension(n.FileName)));

            if (childNode?.FilePath == null) return;

            string childExtension = Path.GetExtension(childNode.FileName).ToLowerInvariant();
            updatedPath = Path.Combine(path, $"{targetLanguage}{childExtension}").Replace("\\", "/");
            fileFormat = childExtension.TrimStart('.');

            archive.GetEntry(updatedPath)?.Delete();

            List<string> betterQuestingHashTags = [];

            foreach (var line in File.ReadLines(childNode.FilePath))
            {
                if (line.StartsWith('#'))
                {
                    betterQuestingHashTags.Add(line);
                }
                else if (line.Contains('='))
                {
                    break;
                }
            }

            using Stream entryStream = archive.CreateEntry(updatedPath).Open();
            using StreamWriter writer = new(entryStream, new UTF8Encoding(false));
            WriteLocalizationContent(writer, strings, fileFormat, betterQuestingHashTags);
        }

        private static void SavePatchouliLocalization(TreeNodeItem node, IEnumerable<LocalizationItem> strings, ZipArchive archive, string targetLanguage)
        {
            var stack = new Stack<TreeNodeItem>();
            stack.Push(node);
            var langRegex = LocaleRegex();

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current.ChildrenNodes.Count == 0 && current.FilePath != null)
                {
                    var pathParts = current.FilePath.Replace("\\", "/").Split('/');
                    var langIndex = Array.FindIndex(pathParts, p => langRegex.IsMatch(p));

                    if (langIndex >= 0)
                    {
                        pathParts[langIndex] = targetLanguage;
                        var updatedPath = string.Join("/", pathParts);
                        var fileFormat = Path.GetExtension(updatedPath).TrimStart('.').ToLowerInvariant();

                        archive.GetEntry(updatedPath)?.Delete();
                        using var entryStream = archive.CreateEntry(updatedPath).Open();
                        using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                        WriteLocalizationContent(writer, strings, fileFormat);
                    }
                }
                else
                {
                    foreach (var child in current.ChildrenNodes)
                        stack.Push(child);
                }
            }
        }

        private static void SaveModsLocalization(TreeNodeItem node, IEnumerable<LocalizationItem> strings, ZipArchive archive, string targetLanguage)
        {
            if (node.FileName == null) return;

            string fileExtension = Path.GetExtension(node.FileName).ToLowerInvariant();
            string updatedPath;
            string fileFormat;

            if (fileExtension is ".json" or ".lang")
            {
                updatedPath = Path.Combine(
                    Path.GetDirectoryName(node.FilePath) ?? "",
                    $"{targetLanguage}{fileExtension}"
                ).Replace("\\", "/");

                fileFormat = fileExtension.TrimStart('.');
            }
            else
            {
                var langRegex = LocaleRegex();
                TreeNodeItem? childNode = node.ChildrenNodes.FirstOrDefault(n =>
                    n.FileName != null && langRegex.IsMatch(Path.GetFileNameWithoutExtension(n.FileName)));

                if (childNode?.FilePath == null) return;

                string childExtension = Path.GetExtension(childNode.FileName).ToLowerInvariant();

                updatedPath = Path.Combine(
                    Path.GetDirectoryName(childNode.FilePath) ?? "",
                    $"{targetLanguage}{childExtension}"
                ).Replace("\\", "/");

                fileFormat = childExtension.TrimStart('.');
            }

            archive.GetEntry(updatedPath)?.Delete();
            using Stream entryStream = archive.CreateEntry(updatedPath).Open();
            using StreamWriter writer = new(entryStream, new UTF8Encoding(false));
            WriteLocalizationContent(writer, strings, fileFormat);
        }

        private static void WriteLocalizationContent(TextWriter writer, IEnumerable<LocalizationItem> strings, string fileFormat, List<string>? hashtags = null)
        {
            switch (fileFormat.ToLowerInvariant())
            {
                case "json":
                    WriteJsonContent(writer, strings);
                    break;
                case "lang":
                    WriteLangContent(writer, strings, hashtags);
                    break;
                case "snbt":
                    WriteSnbtContent(writer, strings);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported file format: {fileFormat}");
            }
        }

        private static void WriteJsonContent(TextWriter writer, IEnumerable<LocalizationItem> strings)
        {
            var entries = strings
                .Where(e => e.ID != null)
                .ToDictionary(e => e.ID!, e =>
                {
                    var value = e.TranslatedString?.Trim() ?? "";

                    if (value.StartsWith('[') || value.StartsWith('{'))
                    {
                        try
                        {
                            return JToken.Parse(value);
                        }
                        catch { }
                    }

                    return e.DataType?.Name switch
                    {
                        nameof(Int64) when long.TryParse(value, out var l) => l,
                        nameof(Double) when double.TryParse(value, out var d) => d,
                        nameof(Boolean) when bool.TryParse(value, out var b) => b,
                        _ => value
                    };
                });

            writer.Write(JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        private static void WriteLangContent(TextWriter writer, IEnumerable<LocalizationItem> strings, List<string>? hashtags)
        {
            if (hashtags?.Count > 0)
            {
                foreach (var hashtag in hashtags)
                {
                    writer.WriteLine(hashtag);
                }

                writer.WriteLine();
            }

            foreach (var entry in strings.Where(e => e?.ID != null))
            {
                writer.WriteLine($"{entry.ID}={entry.TranslatedString}");
            }
        }

        private static void WriteSnbtContent(TextWriter writer, IEnumerable<LocalizationItem> strings)
        {
            writer.WriteLine("{");

            var entries = strings.Where(e => e?.ID != null).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                string formattedValue = SnbtManager.FormatSnbtEntry(entry.TranslatedString);
                string line = $"\t{entry.ID}: {formattedValue}";

                if (line.Contains("default_hide_dependency_lines"))
                    continue;

                if (i < entries.Count - 1)
                    line += ",";

                writer.WriteLine(line);
            }

            writer.WriteLine("}");
        }
    }
}