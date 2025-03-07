using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MinecraftLocalizer.Models.Localization
{
    public class LocalizationSaveManager()
    {

        public void SaveTranslations(List<TreeNodeItem> checkedNodes, ObservableCollection<LocalizationItem> localizationStrings, TranslationModeType modeType)
        {
            if (checkedNodes.Count == 0)
            {
                throw new InvalidOperationException("No checked files for saving.");
            }

            string zipFilePath = Path.Combine("MinecraftLocalizer.zip");

            using FileStream zipStream = new(zipFilePath, FileMode.OpenOrCreate);
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

        private static void AddPackMeta(ZipArchive archive)
        {
            ZipArchiveEntry packMetaEntry = archive.CreateEntry("pack.mcmeta");

            using Stream packMetaStream = packMetaEntry.Open();
            using StreamWriter writer = new(packMetaStream);

            string packMetaContent = "{\"pack\":{\"pack_format\":15,\"description\":\"Перевод мода\"}}";
            writer.Write(packMetaContent);
        }

        private void AddPackImage(ZipArchive archive)
        {
            string packImagePath = Path.Combine( "pack.png");
            ZipArchiveEntry packImageEntry = archive.CreateEntry("pack.png");

            using Stream packImageStream = packImageEntry.Open();
            using FileStream fileStream = new(packImagePath, FileMode.OpenOrCreate, FileAccess.Read);

            fileStream.CopyTo(packImageStream);
        }

        private static void SaveLocalizationForNode(TreeNodeItem node, ObservableCollection<LocalizationItem> localizationStrings, ZipArchive archive, TranslationModeType modeType)
        {
            if (localizationStrings.Count == 0) return;

            string modName = node.FileName;
            if (string.IsNullOrWhiteSpace(modName))
            {
                throw new InvalidOperationException("Failed to determine the mod name for translation saving.");
            }

            string targetLanguageSetting = Properties.Settings.Default.TargetLanguage;
            bool useJsonFormat = modeType == TranslationModeType.Mods;
            string entryPath = $"assets/{modName}/lang/{targetLanguageSetting}.{(useJsonFormat ? "json" : "snbt")}";

            ZipArchiveEntry? existingEntry = archive.GetEntry(entryPath);
            existingEntry?.Delete();

            ZipArchiveEntry entryArchive = archive.CreateEntry(entryPath);

            using Stream entryStream = entryArchive.Open();
            using StreamWriter writer = new(entryStream);

            if (useJsonFormat)
            {
                Dictionary<string, string?> localizationDict = localizationStrings
                    .Where(e => e.ID != null)
                    .ToDictionary(e => e.ID!, e => e.TranslatedString);

                string jsonContent = JsonConvert.SerializeObject(localizationDict, Formatting.Indented);
                writer.Write(jsonContent);
            }

            else
            {
                StringBuilder snbtContent = new();
                snbtContent.AppendLine("{");

                for (int i = 0; i < localizationStrings.Count; i++)
                {
                    LocalizationItem entry = localizationStrings[i];
                    string formattedValue = FormatSnbtEntry(entry.TranslatedString ?? string.Empty);
                    snbtContent.AppendLine($"\t{entry.ID}: {formattedValue}");
                }

                snbtContent.AppendLine("}");
                writer.Write(snbtContent.ToString());
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
                    elements = inner.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
                                    .Select(e => e.Trim())
                                    .ToList();
                }
                else
                {
                    // Otherwise, parse by commas outside quotes
                    elements = SplitSnbtArrayElements(inner);
                }

                // Clean each element: remove leading/trailing quotes, then escape
                List<string>? cleanedElements = elements
                    .Select(elem => EscapeString(CleanItem(elem)))
                    .ToList();

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
