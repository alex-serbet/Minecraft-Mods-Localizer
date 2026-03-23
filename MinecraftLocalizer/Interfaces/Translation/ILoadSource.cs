using MinecraftLocalizer.Models;

namespace MinecraftLocalizer.Interfaces.Translation
{
    public interface ILoadSource
    {
        Task<(List<LocalizationItem> Items, string RawContent)> LoadAsync();
    }
}
