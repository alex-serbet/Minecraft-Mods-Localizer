using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Interfaces.Translation
{
    public interface ITranslationModeNodeLoader
    {
        Task<IEnumerable<TreeNodeItem>> LoadAsync(TranslationModeType modeType);
    }
}



