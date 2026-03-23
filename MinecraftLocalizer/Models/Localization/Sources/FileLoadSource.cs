using MinecraftLocalizer.Models;
using System.IO;
using MinecraftLocalizer.Interfaces.Translation;

namespace MinecraftLocalizer.Models.Localization
{
    public class FileLoadSource : ILoadSource
    {
        private readonly string _filePath;

        public FileLoadSource(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<(List<LocalizationItem> Items, string RawContent)> LoadAsync()
        {
            string content = await File.ReadAllTextAsync(_filePath);
            return LocalizationContentParser.Process(content, Path.GetExtension(_filePath));
        }
    }
}

