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

        public async Task LoadStringsAsync(ILoadSource source)
        {
            LocalizationStrings.Clear();

            var strings = await source.LoadAsync();
            foreach (var item in strings)
            {
                LocalizationStrings.Add(item);
            }

            UpdateRowNumbers();
        }

        private void UpdateRowNumbers()
        {
            for (int i = 0; i < LocalizationStrings.Count; i++)
            {
                LocalizationStrings[i].RowNumber = i + 1;
            }
        }
    }

    public interface ILoadSource
    {
        Task<List<LocalizationItem>> LoadAsync();
    }

    public class FileLoadSource(string filePath) : ILoadSource
    {
        private readonly string _filePath = filePath;

        public async Task<List<LocalizationItem>> LoadAsync()
        {
            string content = await File.ReadAllTextAsync(_filePath);
            return ContentProcessor.Process(content, Path.GetExtension(_filePath));
        }
    }

    public class JarLoadSource(string jarPath, string internalPath) : ILoadSource
    {
        private readonly string _jarPath = jarPath;
        private readonly string _internalPath = internalPath;

        public async Task<List<LocalizationItem>> LoadAsync()
        {
            using var archive = ZipFile.OpenRead(_jarPath);
            var entry = archive.GetEntry(_internalPath) ?? throw new FileNotFoundException();

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string content = await reader.ReadToEndAsync();
            return ContentProcessor.Process(content, Path.GetExtension(_internalPath));
        }
    }

    public class ZipLoadSource(string zipPath, string internalPath) : ILoadSource
    {
        private readonly string _zipPath = zipPath;
        private readonly string _internalPath = internalPath;

        public async Task<List<LocalizationItem>> LoadAsync()
        {
            using var archive = ZipFile.OpenRead(_zipPath);
            var entry = archive.GetEntry(_internalPath)
                ?? throw new FileNotFoundException($"File '{_internalPath}' not found in ZIP archive");

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string content = await reader.ReadToEndAsync();
            return ContentProcessor.Process(content, Path.GetExtension(_internalPath));
        }
    }

    public static class ContentProcessor
    {
        public static List<LocalizationItem> Process(string content, string fileExtension)
        {
            return fileExtension.ToLowerInvariant() switch
            {
                ".json" => ProcessJson(content),
                ".lang" => ProcessLang(content),
                ".snbt" => ProcessSnbt(content),
                _ => throw new NotSupportedException($"Unsupported file format: {fileExtension}")
            };
        }

        private static List<LocalizationItem> ProcessJson(string content)
        {
            var jsonContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(content)
                ?? throw new JsonException("Invalid JSON format");

            return [.. jsonContent.Select(kvp => new LocalizationItem
            {
                ID = kvp.Key,
                OriginalString = kvp.Value?.ToString() ?? string.Empty,
                TranslatedString = kvp.Value?.ToString() ?? string.Empty,
                DataType = kvp.Value?.GetType()
            })];
        }

        private static List<LocalizationItem> ProcessLang(string content)
        {
            return [.. content.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith('#'))
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .Select(parts => new LocalizationItem
                {
                    ID = parts[0].Trim(),
                    OriginalString = parts[1].Trim(),
                    TranslatedString = parts[1].Trim(),
                    DataType = typeof(string)
                })];
        }

        private static List<LocalizationItem> ProcessSnbt(string content)
        {
            try
            {
                var parsedData = SnbtManager.ParseSnbt(content);
                return [.. parsedData.Cast<DictionaryEntry>()
                    .Select(entry =>
                    {
                        var originalValue = entry.Value;
                        var formattedValue = originalValue != null ? FormatSnbtValue(originalValue) : string.Empty;
                        return new LocalizationItem
                        {
                            ID = entry.Key.ToString()!,
                            OriginalString = formattedValue,
                            TranslatedString = formattedValue
                        };
                    })];
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Error processing SNBT: {ex.Message}");
                return [];
            }
        }

        private static string FormatSnbtValue(object value)
        {
            return value switch
            {
                List<string> list => $"[\n{string.Join("\n", list.Select(v => $"\"{v}\""))}\n]",
                _ => value?.ToString() ?? string.Empty
            };
        }
    }
}