using MinecraftLocalizer.Models;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Services.Core;

namespace MinecraftLocalizer.Models.Localization
{
    internal static class LocalizationDialogContext
    {
        public static IDialogService DialogService { get; set; } = new DialogServiceAdapter();
    }
}




