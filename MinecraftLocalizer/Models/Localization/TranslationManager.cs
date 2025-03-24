using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Localization.Requests;
using MinecraftLocalizer.Models.Services;
using System.Text.RegularExpressions;
using System.Windows;

namespace MinecraftLocalizer.Models
{
    public partial class TranslationManager(LocalizationStringManager localizationManager, IProgress<(int current, int total, double percentage)> progress)
    {
        private readonly LocalizationStringManager _localizationManager = localizationManager;
        private readonly IProgress<(int current, int total, double percentage)> _progress = progress;

        public async Task<bool> TranslateSelectedStrings(List<TreeNodeItem> selectedNodes, TranslationModeType modeType, CancellationToken cancellationToken)
        {
            if (selectedNodes.Count == 0) return false;

            foreach (var node in selectedNodes)
            {
                string sourceLanguage = Properties.Settings.Default.SourceLanguage;
                var targetNode = node.ChildrenNodes.FirstOrDefault(n =>
                    n.FileName != null && (n.FileName == $"{sourceLanguage}.json" || n.FileName == $"{sourceLanguage}.snbt"));

                if (targetNode is null)
                {
                    DialogService.ShowError(string.Format(Properties.Resources.SourceLanguageFileMissingMessage, sourceLanguage, node.FileName));
                    continue;
                } 

                await _localizationManager.LoadStringsAsync(targetNode, modeType);

                var selectedStrings = _localizationManager.LocalizationStrings.Where(e => e.IsSelected).ToList();
                if (selectedStrings.Count == 0) continue;

                int total = selectedStrings.Count;
                int translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected);

                _progress.Report((translated, total, 0));

                while (selectedStrings.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = selectedStrings.Take(50).ToList();
                    await TranslateAiStrings(batch, cancellationToken);

                    translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected);
                    _progress.Report((translated, total, (double)translated / total * 100));

                    selectedStrings = [.. _localizationManager.LocalizationStrings.Where(e => e.IsSelected)];
                }

                LocalizationSaveManager.SaveTranslations([node], _localizationManager.LocalizationStrings, modeType);
            }

            return true;
        }

        private static async Task TranslateAiStrings(List<LocalizationItem> entriesToTranslate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var markedTexts = entriesToTranslate.Select((e, i) => ($"@{i} {e.OriginalString}", i)).ToList();
            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => entriesToTranslate[pair.i]);
            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));

            string translatedText = await new TranslationAiRequest().TranslateTextAsync(combinedText, cancellationToken);

            foreach (var (index, text) in ParseTranslatedString(translatedText))
            {
                if (!indexMap.TryGetValue(index, out var entry)) continue;
                if (!IsValidTranslation(entry.OriginalString ?? "", text)) continue;

                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    entry.TranslatedString = text;
                    entry.IsSelected = false;
                });
            }
        }

        private static bool IsValidTranslation(string original, string translated)
        {
            int bracketsOriginal = original.Count(c => c == '[') - original.Count(c => c == ']');
            int bracketsTranslated = translated.Count(c => c == '[') - translated.Count(c => c == ']');
            return bracketsOriginal == bracketsTranslated;
        }

        private static Dictionary<int, string> ParseTranslatedString(string translatedText)
        {
            return RegexTranslatedString()
                .Matches(translatedText)
                .ToDictionary(
                    match => int.Parse(match.Groups[1].Value),
                    match => match.Groups[2].Value.Trim()
                );
        }

        [GeneratedRegex(@"@(\d+)\s+(.+?)(?=\n@|\z)", RegexOptions.Singleline)]
        private static partial Regex RegexTranslatedString();
    }
}