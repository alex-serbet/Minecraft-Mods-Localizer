using MinecraftLocalizer.Models;
using System.IO;
using System.IO.Compression;
using MinecraftLocalizer.Interfaces.Translation;

namespace MinecraftLocalizer.Models.Localization
{
    public abstract class ArchiveLoadSource : ILoadSource
    {
        protected readonly string ArchivePath;
        protected readonly string InternalPath;

        protected ArchiveLoadSource(string archivePath, string internalPath)
        {
            ArchivePath = archivePath;
            InternalPath = internalPath;
        }

        public async Task<(List<LocalizationItem> Items, string RawContent)> LoadAsync()
        {
            using var archive = ZipFile.OpenRead(ArchivePath);
            var entry = archive.GetEntry(InternalPath) ??
                        throw new FileNotFoundException($"File '{InternalPath}' not found in archive");

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);

            string content = await reader.ReadToEndAsync();
            return LocalizationContentParser.Process(content, Path.GetExtension(InternalPath));
        }
    }
}










