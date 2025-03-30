using MinecraftLocalizer.Converters;
using MinecraftLocalizer.Models.Services;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;

namespace MinecraftLocalizer.Models.Localization
{
    public class LocalizationStringManager
    {
        public ObservableCollection<LocalizationItem> LocalizationStrings { get; } = [];

        public async Task LoadStringsAsync(TreeNodeItem selectedNode, TranslationModeType modeType)
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
            ArgumentNullException.ThrowIfNull(selectedNode);

            try
            {
                return modeType switch
                {
                    TranslationModeType.Mods or TranslationModeType.Patchouli => await LoadFromJarAsync(selectedNode),
                    TranslationModeType.Quests => await LoadFromFileAsync(selectedNode.ModPath),
                    _ => throw new NotSupportedException($"Unsupported mode: {modeType}")
                };
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error loading file: {ex.Message}");
                return [];
            }
        }

        private static async Task<List<LocalizationItem>> LoadFromJarAsync(TreeNodeItem selectedNode)
        {
            if (string.IsNullOrWhiteSpace(selectedNode?.ModPath) || string.IsNullOrWhiteSpace(selectedNode?.FilePath))
            {
                DialogService.ShowError("Invalid mod file path or file name.");
                return [];
            }

            try
            {
                string content = await LoadFileFromJarAsync(selectedNode.ModPath, selectedNode.FilePath);
                return ProcessLocalizationData(content, selectedNode.FileName);
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error reading JAR file: {ex.Message}");
                return [];
            }
        }

        private static async Task<List<LocalizationItem>> LoadFromFileAsync(string? modPath)
        {
            if (string.IsNullOrWhiteSpace(modPath))
                throw new InvalidOperationException("ModPath is null or empty");

            try
            {
                string content = await LoadFileContentAsync(modPath);
                return ProcessLocalizationData(content, modPath);
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error reading file: {ex.Message}");
                return [];
            }
        }

        public static async Task<string> LoadFileContentAsync(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}");

            return await File.ReadAllTextAsync(path);
        }

        public static async Task<string> LoadFileFromJarAsync(string jarPath, string filePath)
        {
            if (!File.Exists(jarPath))
                throw new FileNotFoundException($"Jar file not found: {jarPath}");

            using var archive = ZipFile.OpenRead(jarPath);

            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(filePath, StringComparison.OrdinalIgnoreCase)) 
                ?? throw new FileNotFoundException($"File '{filePath}' not found in archive '{jarPath}'.");

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private static List<LocalizationItem> ProcessLocalizationData(string content, string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".json" => ProcessJson(JsonConvert.DeserializeObject<Dictionary<string, object>>(content) ?? []),
                ".snbt" => ProcessSnbt(content),
                _ => throw new NotSupportedException($"Unknown file format: {Path.GetExtension(fileName)}")
            };
        }

        private static List<LocalizationItem> ProcessJson(Dictionary<string, object> jsonContent)
        {
            return [.. jsonContent.Select(kvp => new LocalizationItem
            {
                ID = kvp.Key,
                OriginalString = kvp.Value is string strValue
                    ? strValue 
                    : JsonConvert.SerializeObject(kvp.Value, Formatting.Indented),
                TranslatedString = kvp.Value?.ToString() ?? string.Empty,
                DataType = kvp.Value?.GetType()
            })];
        }

        private static List<LocalizationItem> ProcessSnbt(string snbtContent)
        {
            try
            {
                var parsedData = SnbtManager.ParseSnbt(snbtContent);

                return [.. parsedData.Cast<DictionaryEntry>().Select(entry => new LocalizationItem
                {
                    ID = entry.Key.ToString()!,
                    OriginalString = entry.Value switch
                    {
                        List<string> list => $"[\n{string.Join("\n", list.Select(v => $"\"{v}\""))}\n]", 
                  
                        _ => entry.Value?.ToString() ?? string.Empty
                    },
                    TranslatedString = entry.Value switch
                    {
                        List<string> list => $"[\n{string.Join("\n", list.Select(v => $"\"{v}\""))}\n]",
         
                        _ => entry.Value?.ToString() ?? string.Empty
                    }
                })];
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error processing SNBT: {ex.Message}");
                return [];
            }
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
