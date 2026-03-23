using MinecraftLocalizer.Interfaces.Ai;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Services.Ai;
using MinecraftLocalizer.Models.Services.Core;
using MinecraftLocalizer.Models.Services.Translation;
using MinecraftLocalizer.ViewModels;
using MinecraftLocalizer.Views;

namespace MinecraftLocalizer.Models.Composition
{
    public sealed class ApplicationBootstrapper
    {
        public IGpt4FreeService? Gpt4FreeService { get; private set; }

        public MainWindow CreateMainWindow()
        {
            var dialogService = new DialogServiceAdapter();
            var localizationDocumentStore = new LocalizationDocumentStore();
            var zipService = CreateZipService(dialogService);
            var fileService = CreateFileService(dialogService);
            var requirementsService = CreateRequirementsService(dialogService);
            var modeNodeLoader = CreateModeNodeLoader(dialogService);
            var archiveService = CreateArchiveService();
            var gpt4FreeService = CreateGpt4FreeService(dialogService);

            LocalizationDialogContext.DialogService = dialogService;

            var mainViewModel = new MainViewModel(
                localizationDocumentStore,
                zipService,
                fileService,
                requirementsService,
                modeNodeLoader,
                archiveService,
                dialogService,
                gpt4FreeService);

            Gpt4FreeService = gpt4FreeService;

            return new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        private static IZipService CreateZipService(IDialogService dialogService) =>
            new ZipService(dialogService);

        private static IFileService CreateFileService(IDialogService dialogService) =>
            new FileService(dialogService);

        private static IRequirementsService CreateRequirementsService(IDialogService dialogService) =>
            new RequirementsService(dialogService);

        private static ITranslationModeNodeLoader CreateModeNodeLoader(IDialogService dialogService)
        {
            var questsService = new FtbQuestsService(dialogService);
            var modsService = new ModsService(dialogService);
            var patchouliService = new PatchouliService(dialogService);
            var betterQuestingService = new BetterQuestingService(dialogService);

            return new TranslationModeNodeLoader(
                questsService,
                modsService,
                patchouliService,
                betterQuestingService);
        }

        private static ITranslationArchiveService CreateArchiveService() =>
            new TranslationArchiveService();

        private static IGpt4FreeService CreateGpt4FreeService(IDialogService dialogService) =>
            new Gpt4FreeService(dialogService);
    }
}
