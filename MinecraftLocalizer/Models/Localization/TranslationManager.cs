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


        [GeneratedRegex(@"@(\d+)\s+(.+?)\s+\1@", RegexOptions.Singleline)]
        private static partial Regex RegexTranslatedString();


        public async Task<bool> TranslateSelectedStrings(List<TreeNodeItem> selectedNodes, TranslationModeType modeType, CancellationToken cancellationToken)
        {
            if (selectedNodes.Count == 0)
                return false;

            foreach (var node in selectedNodes)
            {
                string sourceLanguage = Properties.Settings.Default.SourceLanguage;

                var targetNodes = node.ChildrenNodes.Where(n => n.FileName != null && IsLocalizationFile(n.FileName) && IsMatchingLanguageFile(n.FileName, sourceLanguage)).ToList();

                if (targetNodes.Count == 0)
                {
                    var languageFolder = node.ChildrenNodes
                        .FirstOrDefault(n => string.Equals(n.FileName, sourceLanguage, StringComparison.OrdinalIgnoreCase));

                    if (languageFolder != null)
                    {
                        targetNodes = [.. languageFolder.ChildrenNodes.Where(n => n.FileName != null && IsLocalizationFile(n.FileName))];
                    }
                }

                if (targetNodes.Count == 0)
                {
                    DialogService.ShowError(string.Format(Properties.Resources.SourceLanguageFileMissingMessage, sourceLanguage, node.FileName));
                    continue;
                }

                foreach (var targetNode in targetNodes)
                {
                    targetNode.IsTranslating = true;

                    ExpandTranslationBranch(node, targetNode.FilePath);

                    await _localizationManager.LoadStringsAsync(targetNode, modeType);

                    var selectedStrings = _localizationManager.LocalizationStrings.Where(e => e.IsSelected).ToList();
                    if (selectedStrings.Count == 0)
                        continue;

                    int total = selectedStrings.Count;
                    int translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected);

                    _progress.Report((translated, total, 0));

                    while (selectedStrings.Count > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var batch = selectedStrings.Take(50).ToList();

                        await TranslateStrings(batch, cancellationToken);

                        translated = _localizationManager.LocalizationStrings.Count(e => !e.IsSelected);
                        _progress.Report((translated, total, (double)translated / total * 100));

                        selectedStrings = [.. _localizationManager.LocalizationStrings.Where(e => e.IsSelected)];
                    }

                    LocalizationSaveManager.SaveTranslation([targetNode], _localizationManager.LocalizationStrings, modeType);

                    targetNode.IsTranslating = false;
                }
            }

            return true;
        }

        static bool IsLocalizationFile(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsMatchingLanguageFile(string fileName, string sourceLanguage)
        {
            return fileName.Equals($"{sourceLanguage}.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith($"{sourceLanguage}.lang", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals($"{sourceLanguage}.snbt", StringComparison.OrdinalIgnoreCase);
        }

        private static void ExpandTranslationBranch(TreeNodeItem rootNode, string sourceFilePath)
        {
            rootNode.IsExpanded = true;

            var pathParts = sourceFilePath.Split('/');

            for (int i = 0; i < pathParts.Length; i++)
            {
                var matchingNode = rootNode.ChildrenNodes.FirstOrDefault(n => n.FileName.Contains(pathParts[i]));

                if (matchingNode != null)
                    matchingNode.IsExpanded = true;
            }
        }

        private static async Task TranslateStrings(List<LocalizationItem> entriesToTranslate, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var markedTexts = entriesToTranslate.Select((e, i) => ($"@{i} {e.OriginalString} {i}@", i)).ToList();
            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => entriesToTranslate[pair.i]);
            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));

            string translatedText = await new TranslationAiRequest().TranslateTextAsync(combinedText, cancellationToken);

            bool anyMatchFound = false;

            foreach (var (index, text) in ParseTranslatedString(translatedText))
            {
                if (!indexMap.TryGetValue(index, out var entry)) continue;

                bool isValid = IsValidTranslation(entry.OriginalString ?? "", text);
                string finalText = isValid ? text : "";

                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    entry.TranslatedString = finalText;
                    entry.IsSelected = false;
                });

                anyMatchFound = true;
            }

            // This block resets IsSelected if the string is empty or no matches were found during parsing
            if (!anyMatchFound)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (var entry in entriesToTranslate)
                    {
                        entry.IsSelected = false;
                    }
                });
            }
        }

        private static bool IsValidTranslation(string original, string translated)
        {
            int bracketsOriginal = original.Count(c => c == '[') - original.Count(c => c == ']');
            int bracketsTranslated = translated.Count(c => c == '[') - translated.Count(c => c == ']');
            return bracketsOriginal == bracketsTranslated;
        }

        private static List<KeyValuePair<int, string>> ParseTranslatedString(string translatedText)
        {
            var list = new List<KeyValuePair<int, string>>();


            foreach (Match match in RegexTranslatedString().Matches(translatedText))
            {
                int key = int.Parse(match.Groups[1].Value);
                string value = match.Groups[2].Value.Trim();

                list.Add(new KeyValuePair<int, string>(key, value));
            }

            return list;
        }
    }
}