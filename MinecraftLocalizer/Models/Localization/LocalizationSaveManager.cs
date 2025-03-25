using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;
using MinecraftLocalizer.Models.Services;
using System.Windows.Resources;
using System.Reflection;

namespace MinecraftLocalizer.Models.Localization
{
    public class LocalizationSaveManager()
    {
        public static void SaveTranslations(List<TreeNodeItem> checkedNodes, ObservableCollection<LocalizationItem> localizationStrings, TranslationModeType modeType)
        {
            if (checkedNodes.Count == 0)
            {
                throw new InvalidOperationException(Properties.Resources.NoCheckedFilesSavingMessage);
            }

            try
            {
                string zipModPath = Path.Combine(Properties.Settings.Default.DirectoryPath, "resourcepacks", "MinecraftLocalizer.zip");

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
            catch ( Exception ex)
            {
                DialogService.ShowError($"Failed to save translation. \n{ex.Message}");
            }
        }

        private static void AddPackMeta(ZipArchive archive)
        {
            ZipArchiveEntry packMetaEntry = archive.CreateEntry("pack.mcmeta");

            using Stream packMetaStream = packMetaEntry.Open();
            using StreamWriter writer = new(packMetaStream);

            string packMetaContent =
                "{\n" +
                    "\t\"pack\": {\n" +
                        "\t\t\"pack_format\": 8,\n" + 
                        "\t\t\"supported_formats\": [8, 9999],\n" +
                        $"\t\t\"description\": \"§eLocalization for [{Properties.Settings.Default.TargetLanguage}]\\n§bMade by alex-serbet\"\n" +  
                    "\t}\n" +
                "}";

            writer.Write(packMetaContent);
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
            if (localizationStrings.Count == 0) return;

            string modName = node.FileName;
            if (string.IsNullOrWhiteSpace(modName))
                throw new InvalidOperationException("Failed to determine the mod name for translation saving.");

            var extension = Path.GetExtension(node.ChildrenNodes[0].FileName)?.ToLowerInvariant();
            bool isJsonFormat = extension == ".json";
            string targetLanguage = Properties.Settings.Default.TargetLanguage;

            if (modeType == TranslationModeType.Quests)
            {
                string path = isJsonFormat
                    ? Path.Combine(Properties.Settings.Default.DirectoryPath, "kubejs", "assets", "kubejs", "lang")
                    : Path.Combine(Properties.Settings.Default.DirectoryPath, "config", "ftbquests", "quests", "lang");

                Directory.CreateDirectory(path);
                string outputFile = Path.Combine(path, $"{targetLanguage}.{(isJsonFormat ? "json" : "snbt")}");

                using StreamWriter writer = new(outputFile, false, Encoding.UTF8);
                WriteLocalizationContent(writer, localizationStrings, isJsonFormat);
            }
            else
            {
                string entryPath = $"assets/{modName}/lang/{targetLanguage}.{(isJsonFormat ? "json" : "snbt")}";
                archive.GetEntry(entryPath)?.Delete();

                ZipArchiveEntry entryArchive = archive.CreateEntry(entryPath);
                using Stream entryStream = entryArchive.Open();
                using StreamWriter writer = new(entryStream);

                WriteLocalizationContent(writer, localizationStrings, isJsonFormat);
            }
        }

        private static void WriteLocalizationContent(TextWriter writer, ObservableCollection<LocalizationItem> localizationStrings, bool isJsonFormat)
        {
            if (isJsonFormat)
            {
                var localizationDict = localizationStrings
                    .Where(e => e.ID != null)
                    .ToDictionary(e => e.ID!, e => e.TranslatedString);

                writer.Write(JsonConvert.SerializeObject(localizationDict, Formatting.Indented));
            }
            else
            {
                writer.WriteLine("{");
                foreach (var entry in localizationStrings)
                {
                    writer.WriteLine($"\t{entry.ID}: {FormatSnbtEntry(entry.TranslatedString ?? "")}");
                }
                writer.WriteLine("}");
            }
        }


        /// <summary>
        /// Formats an SNBT string for saving.
        /// If the string starts with "[" and ends with "]", it is considered an array.
        /// If the array contains a single element, it is displayed on one line.
        /// If multiple elements exist, each is displayed on a new line.
        /// Leading and trailing quotes are removed, and escaping is applied afterward.
        /// </summary>
        private static string FormatSnbtEntry(string translatedText)
        {
            string trimmed = translatedText.Trim();
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                // Remove outer square brackets
                string inner = trimmed[1..^1].Trim();
                List<string> elements;

                // If line breaks are present, split by them
                if (inner.Contains('\n'))
                {
                    elements = [.. inner.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim())];
                }
                else
                {
                    // Otherwise, parse by commas outside quotes
                    elements = SplitSnbtArrayElements(inner);
                }

                // Clean each element: remove leading/trailing quotes, then escape
                List<string>? cleanedElements = [.. elements.Select(elem => EscapeString(CleanItem(elem)))];

                if (cleanedElements.Count == 1)
                {
                    // Single element – output in one line
                    return $"[\"{cleanedElements[0]}\"]";
                }
                else
                {
                    // Multiple elements – each on a new line
                    StringBuilder sb = new();
                    sb.AppendLine("[");
                    foreach (string item in cleanedElements)
                    {
                        sb.AppendLine($"\t\t\"{item}\"");
                    }

                    sb.Append("\t]");
                    return sb.ToString();
                }
            }
            else
            {
                // If not an array – just remove extra quotes and escape
                return $"\"{EscapeString(CleanItem(translatedText))}\"";
            }
        }

        /// <summary>
        /// Splits a string representing SNBT array contents
        /// into separate elements, dividing by commas outside quotes.
        /// </summary>
        private static List<string> SplitSnbtArrayElements(string input)
        {
            List<string> result = [];
            StringBuilder current = new();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    current.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            if (current.Length > 0)
            {
                result.Add(current.ToString().Trim());
            }

            return result;
        }

        /// <summary>
        /// Removes all leading and trailing quotes (both normal and escaped) from a string.
        /// </summary>
        private static string CleanItem(string s)
        {
            s = s.Trim();
            // Remove leading quotes
            while (s.StartsWith("\\\"") || s.StartsWith('\"'))
            {
                if (s.StartsWith("\\\""))
                    s = s[2..];
                else if (s.StartsWith('\"'))
                    s = s[1..];
                s = s.TrimStart();
            }
            // Remove trailing quotes
            while (s.EndsWith("\\\"") || s.EndsWith('\"'))
            {
                if (s.EndsWith("\\\""))
                    s = s[..^2];
                else if (s.EndsWith('\"'))
                    s = s[..^1];
                s = s.TrimEnd();
            }

            return s;
        }

        /// <summary>
        /// Escapes a string for SNBT:
        /// – replaces backslashes with double backslashes,
        /// – newline characters with the literal "\\n",
        /// – escapes quotes.
        /// </summary>
        private static string EscapeString(string input)
        {
            // First, replace backslashes
            string result = input.Replace("\\", "\\\\");
            // Then replace newlines with the literal \n
            result = result.Replace("\n", "\\n");
            // Escape quotes
            result = result.Replace("\"", "\\\"");
            return result;
        }
    }
}
