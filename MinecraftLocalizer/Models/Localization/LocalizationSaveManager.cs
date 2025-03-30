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

        private const string PackDescriptionTemplate = "§eLocalization for [{0}]\\n§bMade by alex-serbet";

        private const string PackMetaContent = """
            {
                "pack": {
                    "pack_format": 8,
                    "supported_formats": [8, 9999],
                    "description": "{0}"
                }
            }
            """;


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
            ZipArchiveEntry packMetaEntry = archive.CreateEntry("pack.mcmeta");

            using Stream packMetaStream = packMetaEntry.Open();
            using StreamWriter writer = new(packMetaStream);

            string description = PackDescriptionTemplate.Replace("{", "{{").Replace("}", "}}");
            string formattedDescription = string.Format(description, Settings.TargetLanguage);

            if (!PackMetaContent.Contains("{0}"))
                throw new FormatException("PackMetaContent does not contain a valid format placeholder '{0}'.");

            string packMetaContent = PackMetaContent.Replace("{", "{{").Replace("}", "}}");
            string content = string.Format(packMetaContent, formattedDescription);

            writer.Write(JsonConvert.SerializeObject(JsonConvert.DeserializeObject(content), Formatting.Indented));
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
            if (localizationStrings.Count == 0 || node?.FileName == null) return;

            var targetLanguage = Settings.TargetLanguage;
            var isJsonFormat = Path.GetExtension(node.FileName)?.Equals(".json", StringComparison.OrdinalIgnoreCase) ?? false;

            switch (modeType)
            {
                case TranslationModeType.Quests:
                    SaveQuestLocalization(node, localizationStrings, isJsonFormat);
                    break;

                case TranslationModeType.Patchouli:
                    SavePatchouliLocalization(node, localizationStrings, archive, targetLanguage);
                    break;

                case TranslationModeType.Mods:
                    SaveModsLocalization(node, localizationStrings, archive, targetLanguage);
                    break;
            }
        }

        private static void SaveQuestLocalization(TreeNodeItem node, IEnumerable<LocalizationItem> strings, bool isJsonFormat)
        {
            ArgumentNullException.ThrowIfNull(node);

            var basePath = isJsonFormat
                ? Path.Combine(Settings.DirectoryPath, "kubejs", "assets", "kubejs", "lang")
                : Path.Combine(Settings.DirectoryPath, "config", "ftbquests", "quests", "lang");

            Directory.CreateDirectory(basePath);
            var outputFile = Path.Combine(basePath, $"{Settings.TargetLanguage}.{(isJsonFormat ? "json" : "snbt")}");

            using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false));
            WriteLocalizationContent(writer, strings, isJsonFormat);
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

                        archive.GetEntry(updatedPath)?.Delete();
                        using var entryStream = archive.CreateEntry(updatedPath).Open();
                        using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                        WriteLocalizationContent(writer, strings, true);
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
            bool isJsonFormat;
            string updatedPath;

            if (!node.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                var langRegex = LocaleRegex();
                TreeNodeItem? childNode = node.ChildrenNodes.FirstOrDefault(n =>
                    n.FileName != null &&
                    langRegex.IsMatch(Path.GetFileNameWithoutExtension(n.FileName)));

                if (childNode?.FilePath == null)
                    return;

                updatedPath = Path.Combine(
                    Path.GetDirectoryName(childNode.FilePath) ?? "",
                    $"{targetLanguage}{Path.GetExtension(childNode.FileName)}"
                ).Replace("\\", "/");

                isJsonFormat = Path.GetExtension(childNode.FileName)?.Equals(".json", StringComparison.OrdinalIgnoreCase) ?? false;
            }
            else
            {
                updatedPath = Path.Combine(
                    Path.GetDirectoryName(node.FilePath) ?? "",
                    $"{targetLanguage}{Path.GetExtension(node.FileName)}"
                ).Replace("\\", "/");

                isJsonFormat = true;
            }

            archive.GetEntry(updatedPath)?.Delete();
            using Stream entryStream = archive.CreateEntry(updatedPath).Open();
            using StreamWriter writer = new(entryStream, new UTF8Encoding(false));
            WriteLocalizationContent(writer, strings, isJsonFormat);
        }

        private static void WriteLocalizationContent(TextWriter writer, IEnumerable<LocalizationItem> strings, bool isJson)
        {
            if (isJson)
            {
                WriteJsonContent(writer, strings);
            }
            else
            {
                WriteSnbtContent(writer, strings);
            }
        }

        private static void WriteJsonContent(TextWriter writer, IEnumerable<LocalizationItem> strings)
        {
            var entries = strings
                    .Where(e => e.ID != null)
                    .ToDictionary(e => e.ID!, e => ConvertLocalizationValue(e));
            writer.Write(JsonConvert.SerializeObject(entries, Formatting.Indented));
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

        private static object ConvertLocalizationValue(LocalizationItem e)
        {
            var value = e.TranslatedString?.Trim() ?? "";

            if (value.StartsWith('[') || value.StartsWith('{'))
            {
                try
                {
                    return JToken.Parse(value);
                }
                catch {}
            }

            return e.DataType?.Name switch
            {
                nameof(Int64) when long.TryParse(value, out var l) => l,
                nameof(Double) when double.TryParse(value, out var d) => d,
                nameof(Boolean) when bool.TryParse(value, out var b) => b,
                _ => value
            };
        }
    }
}
