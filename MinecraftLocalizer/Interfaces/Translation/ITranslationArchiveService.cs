using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Interfaces.Translation
{
    public interface ITranslationArchiveService
    {
        string GetPatchouliArchivePath();

        bool TrySave(
            List<TreeNodeItem> checkedNodes,
            IEnumerable<LocalizationItem> localizationStrings,
            string localizationText,
            TranslationModeType modeType,
            bool isRawViewMode,
            out string? archivePath);
    }
}



