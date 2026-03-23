using MinecraftLocalizer.Interfaces.Translation;

namespace MinecraftLocalizer.Models.Services.Translation
{
    public sealed class TranslationModeNodeLoader : ITranslationModeNodeLoader
    {
        private readonly FtbQuestsService _questsService;
        private readonly ModsService _modsService;
        private readonly PatchouliService _patchouliService;
        private readonly BetterQuestingService _betterQuestingService;

        public TranslationModeNodeLoader(
            FtbQuestsService questsService,
            ModsService modsService,
            PatchouliService patchouliService,
            BetterQuestingService betterQuestingService)
        {
            _questsService = questsService;
            _modsService = modsService;
            _patchouliService = patchouliService;
            _betterQuestingService = betterQuestingService;
        }

        public async Task<IEnumerable<TreeNodeItem>> LoadAsync(TranslationModeType modeType)
        {
            return modeType switch
            {
                TranslationModeType.Quests => await _questsService.LoadQuestsNodesAsync(),
                TranslationModeType.Mods => await _modsService.LoadModsNodesAsync(),
                TranslationModeType.ResourcePack => [],
                TranslationModeType.Patchouli => await _patchouliService.LoadPatchouliNodesAsync(),
                TranslationModeType.BetterQuesting => await _betterQuestingService.LoadQuestsNodesAsync(),
                _ => []
            };
        }
    }
}




