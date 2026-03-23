using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Properties;
using System.Collections.ObjectModel;
using System.IO;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public sealed class TranslationArchiveService : ITranslationArchiveService
    {
        public string GetPatchouliArchivePath() =>
            Path.Combine(Settings.Default.DirectoryPath, "resourcepacks", "MinecraftLocalizer.zip");

        public bool TrySave(
            List<TreeNodeItem> checkedNodes,
            IEnumerable<LocalizationItem> localizationStrings,
            string localizationText,
            TranslationModeType modeType,
            bool isRawViewMode,
            out string? archivePath)
        {
            archivePath = null;

            if (checkedNodes.Count == 0 || modeType == TranslationModeType.NotSelected)
                return false;

            if (modeType == TranslationModeType.Patchouli)
            {
                archivePath = GetPatchouliArchivePath();
                return File.Exists(archivePath);
            }

            archivePath = LocalizationArchiveWriter.SaveTranslation(
                checkedNodes,
                new ObservableCollection<LocalizationItem>(localizationStrings),
                localizationText,
                modeType,
                isRawViewMode);

            return !string.IsNullOrWhiteSpace(archivePath);
        }
    }
}





