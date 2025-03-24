using MinecraftLocalizer.Converters;
using MinecraftLocalizer.Models.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MinecraftLocalizer.Models.Localization
{
    public class LocalizationStringManager
    {
        public ObservableCollection<LocalizationItem> LocalizationStrings { get; private set; } = [];

        public  async Task LoadStringsAsync(TreeNodeItem selectedNode, TranslationModeType modeType)
        {
            LocalizationStrings.Clear();
            var strings = await LoadLocalizationStringsAsync(selectedNode, modeType);
            foreach (var item in strings)
            {
                LocalizationStrings.Add(item);
            }
            UpdateRowNumbers();
        }

        private static async Task<List<LocalizationItem>> LoadLocalizationStringsAsync(TreeNodeItem selectedNode, TranslationModeType modeType)
        {
            List<LocalizationItem> localizationStrings = [];

            try
            {
                if (modeType == TranslationModeType.Mods)
                {
                    localizationStrings = await LoadFromZipAsync(selectedNode.ModPath, selectedNode.FileName);
                }
                else if (modeType == TranslationModeType.Quests)
                {
                    string ModPath = selectedNode.ModPath;
                    string extension = Path.GetExtension(ModPath).ToLower();

                    localizationStrings = extension switch
                    {
                        ".json" => await LoadFromJsonAsync(ModPath),
                        ".snbt" => await LoadFromSnbtAsync(ModPath),
                        _ => throw new NotSupportedException($"Unknown file format: {extension}")
                    };
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error loading file: {ex.Message}");
            }

            return localizationStrings;
        }

        private static async Task<List<LocalizationItem>> LoadFromZipAsync(string modModPath, string fileName)
        {
            List<LocalizationItem> localizationStrings = [];

            using var archive = ZipFile.OpenRead(modModPath);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

            if (entry != null)
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                string content = await reader.ReadToEndAsync();
                string extension = Path.GetExtension(fileName).ToLower();

                localizationStrings = extension switch
                {
                    ".json" => ProcessJson(content),
                    ".snbt" => ProcessSnbt(content),
                    _ => []
                };
            }
            else
            {
                DialogService.ShowError($"File '{fileName}' not found in archive '{modModPath}'.");
            }

            return localizationStrings;
        }

        private static async Task<List<LocalizationItem>> LoadFromJsonAsync(string ModPath) =>
            ProcessJson(await File.ReadAllTextAsync(ModPath));

        private static async Task<List<LocalizationItem>> LoadFromSnbtAsync(string ModPath) =>
            ProcessSnbt(await File.ReadAllTextAsync(ModPath));

        private static List<LocalizationItem> ProcessJson(string jsonContent)
        {
            var localizationStrings = new List<LocalizationItem>();
            var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

            if (jsonDict != null)
            {
                foreach (var kvp in jsonDict)
                {
                    string? originalText = kvp.Value is JArray array
                        ? $"[\n{string.Join(",\n", array.Select(v => $"\"{v}\""))}\n]"
                        : kvp.Value.ToString();

                    localizationStrings.Add(new LocalizationItem
                    {
                        ID = kvp.Key,
                        OriginalString = originalText,
                        TranslatedString = originalText,
                    });
                }
            }

            return localizationStrings;
        }

        private static List<LocalizationItem> ProcessSnbt(string snbtContent)
        {
            List<LocalizationItem> localizationStrings = [];

            try
            {
                var parsedData = SnbtParser.ParseSnbt(snbtContent);
                foreach (DictionaryEntry entry in parsedData)
                {
                    string? key = entry.Key.ToString();
                    string value = entry.Value switch
                    {
                        List<string> listValue => $"[\n{string.Join("\n", listValue.Select(v => $"\"{v}\""))}\n]",
                        string stringValue => stringValue,
                        _ => entry.Value?.ToString() ?? string.Empty
                    };

                    localizationStrings.Add(new LocalizationItem
                    {
                        ID = key,
                        OriginalString = value,
                        TranslatedString = value
                    });
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error processing SNBT: {ex.Message}");
            }

            return localizationStrings;
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < LocalizationStrings.Count; i++)
            {
                LocalizationStrings[i].RowNumber = i + 1;
            }
        }
    }
}
