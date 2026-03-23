using MinecraftLocalizer.Models;
using MinecraftLocalizer.Properties;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class LocalizationArchiveWriter
    {
        private static readonly Settings Settings = Settings.Default;
        private static bool _isRawViewMode;

        [GeneratedRegex(@"^[a-z]{2}_[a-z]{2}$", RegexOptions.IgnoreCase)]
        private static partial Regex LocaleRegex();

        public static string SaveTranslation(
            List<TreeNodeItem> checkedNodes,
            ObservableCollection<LocalizationItem> localizationStrings,
            string localizationText,
            TranslationModeType modeType,
            bool isRawViewMode)
        {
            if (checkedNodes.Count == 0)
                throw new InvalidOperationException(Resources.NoCheckedFilesSavingMessage);

            _isRawViewMode = isRawViewMode;

            try
            {
                if (modeType == TranslationModeType.Quests)
                {
                    string? savedQuestPath = null;
                    foreach (var node in checkedNodes)
                    {
                        var path = SaveQuestFile(node, localizationStrings);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            savedQuestPath = path;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(savedQuestPath))
                    {
                        throw new InvalidOperationException("Quest localization file path could not be determined.");
                    }

                    return savedQuestPath;
                }

                if (modeType == TranslationModeType.BetterQuesting)
                {
                    string? savedPath = null;
                    foreach (var node in checkedNodes)
                    {
                        var path = SaveBetterQuestingFile(node, localizationStrings, Settings.TargetLanguage);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            savedPath = path;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(savedPath))
                    {
                        throw new InvalidOperationException("BetterQuesting localization file path could not be determined.");
                    }

                    return savedPath;
                }

                string resourcePacksDir = Path.Combine(Settings.DirectoryPath, "resourcepacks");
                Directory.CreateDirectory(resourcePacksDir);
                string zipPath = Path.Combine(resourcePacksDir, "MinecraftLocalizer.zip");

                using var zipStream = new FileStream(zipPath, FileMode.OpenOrCreate);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Update, true);

                EnsureResourcePackMetadata(archive);

                foreach (var node in checkedNodes)
                {
                    SaveNodeLocalization(node, localizationStrings, localizationText, archive, modeType);
                }

                return zipPath;
            }
            catch (Exception ex)
            {
                LocalizationDialogContext.DialogService.ShowError($"Failed to save translation. \n{ex.Message}");
                throw;
            }
        }

        private static void EnsureResourcePackMetadata(ZipArchive archive)
        {
            if (!archive.Entries.Any(e => e.FullName == "pack.mcmeta"))
            {
                AddPackMetadata(archive);
                AddPackIcon(archive);
            }
        }

        private static void AddPackMetadata(ZipArchive archive)
        {
            var entry = archive.CreateEntry("pack.mcmeta");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);

            string description = $"§eLocalization for [{Settings.TargetLanguage}]\n§bMade by alex-serbet";
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

        private static void AddPackIcon(ZipArchive archive)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var resourceStream = assembly.GetManifestResourceStream("MinecraftLocalizer.Assets.pack.png");

            if (resourceStream != null)
            {
                var entry = archive.CreateEntry("pack.png");
                using var entryStream = entry.Open();
                resourceStream.CopyTo(entryStream);
            }
        }

       
    }
}




