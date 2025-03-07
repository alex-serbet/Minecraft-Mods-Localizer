using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Localization.Requests;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftLocalizer.Models
{
    public partial class TranslationManager(LocalizationStringManager localizationManager, IProgress<(int current, int total, double percentage)> progress)
    {
        private readonly LocalizationStringManager _localizationManager = localizationManager;
        private readonly LocalizationSaveManager _localizationSaver = new();

        private readonly IProgress<(int current, int total, double percentage)> _progress = progress;

        public async Task<bool> TranslateSelectedStrings(List<TreeNodeItem> selectedNodes, TranslationModeType modeType, CancellationToken cancellationToken)
        {
            if (selectedNodes.Count == 0)
                return false;

            foreach (var node in selectedNodes)
            {
                TreeNodeItem? targetNode = node.ChildrenNodes.FirstOrDefault(n => n.FileName == "en_us.json" || n.FileName == "en_us.snbt");

                if (targetNode != null)
                {
                    await _localizationManager.LoadStringsAsync(targetNode, modeType);

                    var selectedStrings = _localizationManager.LocalizationStrings.Where(e => e.IsSelected).ToList();
                    if (selectedStrings.Count == 0) continue;

                    // Количество всех строк, которые были выбраны для перевода
                    int total = selectedStrings.Count;

                    // Оригинальный список для подсчета переведенных строк
                    int translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected);  // Считаем строки, которые не выбраны (переведены)

                    _progress.Report((translated, total, 0));

                    while (selectedStrings.Count > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = selectedStrings.Take(50).ToList();

                        await TranslateBatch(batch, cancellationToken);

                        // Обновляем количество переведенных строк в оригинальном списке
                        translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected); // Количество переведенных строк = те, у которых IsSelected == false

                        _progress.Report((translated, total, (double)translated / total * 100));

                        // Обновляем список строк, которые еще не переведены
                        selectedStrings = _localizationManager.LocalizationStrings.Where(e => e.IsSelected).ToList();
                    }

                    _localizationSaver.SaveTranslations([node], _localizationManager.LocalizationStrings, modeType);
                }
            }

            return true;
        }





        private static async Task TranslateBatch(List<LocalizationItem> entriesToTranslate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var markedTexts = entriesToTranslate
                .Select((e, i) => ($"@{i} {e.OriginalString}", i))
                .ToList();

            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => entriesToTranslate[pair.i]);
            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));


            string translatedText = await new TranslationAiRequest().TranslateTextAsync(combinedText, cancellationToken);

            foreach (var (index, text) in ParseTranslatedString(translatedText))
            {
                if (indexMap.TryGetValue(index, out var entry))
                {
                    if (entry.OriginalString != null && !IsValidTranslation(entry.OriginalString, text))
                    {
                        continue;
                    }

                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        entry.TranslatedString = text;
                        entry.IsSelected = false;
                    });
                }
            }
        }

        /// <summary>
        /// Проверяет, совпадает ли количество квадратных скобок в оригинале и переводе.
        /// </summary>
        private static bool IsValidTranslation(string original, string translated)
        {
            int countOriginal = original.Count(c => c == '[') - original.Count(c => c == ']');
            int countTranslated = translated.Count(c => c == '[') - translated.Count(c => c == ']');

            return countOriginal == countTranslated;
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