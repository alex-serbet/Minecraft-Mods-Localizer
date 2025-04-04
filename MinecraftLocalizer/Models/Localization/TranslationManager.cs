using MinecraftLocalizer.Models.Localization.Requests;
using MinecraftLocalizer.Models.Services;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace MinecraftLocalizer.Models.Localization

{    /// <summary>
     /// Manages the translation process for selected localization strings.
     /// </summary>
    public partial class TranslationManager(LocalizationStringManager localizationManager, IProgress<(int current, int total, double percentage)> progress)
    {
        private readonly LocalizationStringManager _localizationManager = localizationManager;
        private readonly IProgress<(int current, int total, double percentage)> _progress = progress;

        private static readonly ConcurrentDictionary<string, ILoadSource> LoadSourceCache = new();
        private static readonly Regex TranslatedStringRegex = new(@"@(\d+)\s+(.+?)\s+\1@", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));

        private static readonly Dictionary<string, string[]> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            [".json"] = [".json"],
            [".lang"] = [".lang"],
            [".snbt"] = [".snbt"]
        };

        public async Task<bool> TranslateSelectedStrings(
            List<TreeNodeItem> selectedNodes,
            TranslationModeType modeType,
            CancellationToken cancellationToken)
        {
            if (selectedNodes.Count == 0)
                return false;

            string sourceLanguage = Properties.Settings.Default.SourceLanguage;
            var nodePairs = CollectTargetNodes(selectedNodes, sourceLanguage);
            if (nodePairs.Count == 0)
                return false;

            int totalStrings = await CalculateTotalStringsAsync(nodePairs);
            if (totalStrings == 0)
                return false;

            return await ProcessTranslationsAsync(nodePairs, modeType, totalStrings, cancellationToken);
        }

        private static List<(TreeNodeItem Source, TreeNodeItem Target)> CollectTargetNodes(List<TreeNodeItem> nodes, string sourceLanguage)
        {
            var pairs = new List<(TreeNodeItem, TreeNodeItem)>();

            foreach (var node in nodes)
            {
                var targetNodes = GetTargetNodes(node, sourceLanguage);
                if (targetNodes.Count == 0)
                {
                    if (IsMatchingLanguageFile(node.FileName, sourceLanguage))
                    {
                        targetNodes.Add(node);
                    }
                    else
                    {
                        DialogService.ShowError(string.Format(
                            Properties.Resources.SourceLanguageFileMissingMessage,
                            sourceLanguage,
                            node.FileName));
                        continue;
                    }
                }
                pairs.AddRange(targetNodes.Select(t => (node, t)));
            }

            return pairs;
        }

        public static List<TreeNodeItem> GetTargetNodes(TreeNodeItem node, string sourceLanguage)
        {
            var targetNodes = node.ChildrenNodes
                .Where(n => n.FileName != null && IsLocalizationFile(n.FileName) && IsMatchingLanguageFile(n.FileName, sourceLanguage))
                .ToList();

            if (targetNodes.Count == 0)
            {
                targetNodes = [.. node.ChildrenNodes.SelectMany(GetAllLocalizationFiles)];
            }

            if (targetNodes.Count == 0)
            {
                var languageFolder = node.ChildrenNodes.FirstOrDefault(n =>
                    string.Equals(n.FileName, sourceLanguage, StringComparison.OrdinalIgnoreCase));
                if (languageFolder != null)
                {
                    targetNodes = [.. languageFolder.ChildrenNodes.Where(n => n.FileName != null && IsLocalizationFile(n.FileName))];
                }
            }

            return targetNodes;
        }

        private async Task<int> CalculateTotalStringsAsync(List<(TreeNodeItem Source, TreeNodeItem Target)> pairs)
        {
            int total = 0;
            foreach (var (source, target) in pairs)
            {
                await LoadStringsAsync(source, target);
                total += _localizationManager.LocalizationStrings.Count(e => e.IsSelected);
            }
            return total;
        }

        private async Task<bool> ProcessTranslationsAsync(
            List<(TreeNodeItem Source, TreeNodeItem Target)> pairs,
            TranslationModeType modeType,
            int totalStrings,
            CancellationToken cancellationToken)
        {
            int translatedCount = 0;
            _progress.Report((0, totalStrings, 0));

            foreach (var (source, target) in pairs)
            {
                target.IsTranslating = true;
                ExpandTranslationBranch(source, target.FilePath);

                await LoadStringsAsync(source, target);
                var selectedStrings = _localizationManager.LocalizationStrings.Where(e => e.IsSelected).ToList();

                while (selectedStrings.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = selectedStrings.Take(50).ToList();
                    int translatedThisBatch = await ProcessTranslationBatch(batch, cancellationToken);
                    translatedCount += translatedThisBatch;
                    _progress.Report((translatedCount, totalStrings, (double)translatedCount / totalStrings * 100));

                    selectedStrings = [.. _localizationManager.LocalizationStrings.Where(e => e.IsSelected)];
                }

                await SaveTranslations([target], modeType);
                target.IsTranslating = false;
            }

            return true;
        }

        private static async Task<int> ProcessTranslationBatch(List<LocalizationItem> batch, CancellationToken cancellationToken)
        {
            int translatedInBatch = 0;

            // Prepare marked text for translation
            var markedTexts = batch.Select((item, i) => ($"@{i} {item.OriginalString} {i}@", i)).ToList();
            var indexMap = markedTexts.ToDictionary(pair => pair.i, pair => batch[pair.i]);
            string combinedText = string.Join("\n", markedTexts.Select(pair => pair.Item1));

            string translatedText = await new TranslationAiRequest().TranslateTextAsync(combinedText, cancellationToken);
            bool anyMatchFound = false;

            foreach (var (index, text) in ParseTranslatedString(translatedText))
            {
                if (!indexMap.TryGetValue(index, out var entry))
                    continue;

                bool isValid = IsValidTranslation(entry.OriginalString ?? string.Empty, text);
                string finalText = isValid ? text : string.Empty;

                if (isValid)
                    translatedInBatch++;

                await UpdateUIAsync(() =>
                {
                    entry.TranslatedString = finalText;
                    entry.IsSelected = false;
                });

                anyMatchFound = true;
            }

            // If no valid matches were found, deselect entries
            if (!anyMatchFound)
            {
                await UpdateUIAsync(() =>
                {
                    foreach (var entry in batch)
                    {
                        entry.IsSelected = false;
                    }
                });
            }

            return translatedInBatch;
        }

        private async Task SaveTranslations(List<TreeNodeItem> nodes, TranslationModeType modeType)
        {
            await Task.Run(() =>
            {
                LocalizationSaveManager.SaveTranslation(
                    nodes,
                    _localizationManager.LocalizationStrings,
                    modeType);
            });
        }

        private static List<KeyValuePair<int, string>> ParseTranslatedString(string translatedText)
        {
            return [.. TranslatedStringRegex.Matches(translatedText)
                .Select(m => new KeyValuePair<int, string>(
                    int.Parse(m.Groups[1].Value),
                    m.Groups[2].Value.Trim()))];
        }

        private async Task LoadStringsAsync(TreeNodeItem sourceNode, TreeNodeItem targetNode)
        {
            var loadSource = LoadSourceCache.GetOrAdd(
                $"{sourceNode.FilePath}|{targetNode.FilePath}",
                _ => CreateLoadSource(sourceNode, targetNode));
            await _localizationManager.LoadStringsAsync(loadSource);
        }

        private static ILoadSource CreateLoadSource(TreeNodeItem sourceNode, TreeNodeItem targetNode)
        {
            return Path.GetExtension(sourceNode.FilePath) switch
            {
                ".zip" => new ZipLoadSource(sourceNode.FilePath, targetNode.FilePath),
                ".jar" => new JarLoadSource(sourceNode.FilePath, targetNode.FilePath),
                _ => new FileLoadSource(targetNode.FilePath)
            };
        }

        private static IEnumerable<TreeNodeItem> GetAllLocalizationFiles(TreeNodeItem node)
        {
            foreach (var child in node.ChildrenNodes)
            {
                if (IsLocalizationFile(child.FileName))
                    yield return child;

                foreach (var subChild in GetAllLocalizationFiles(child))
                    yield return subChild;
            }
        }

        private static bool IsLocalizationFile(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return SupportedExtensions.ContainsKey(ext);
        }

        private static bool IsMatchingLanguageFile(string fileName, string sourceLanguage)
        {
            return fileName.Equals($"{sourceLanguage}.json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith($"{sourceLanguage}.lang", StringComparison.OrdinalIgnoreCase) ||
                   fileName.Equals($"{sourceLanguage}.snbt", StringComparison.OrdinalIgnoreCase);
        }

        private static void ExpandTranslationBranch(TreeNodeItem rootNode, string sourceFilePath)
        {
            string[] pathParts = sourceFilePath.Split('/');
            TreeNodeItem currentNode = rootNode;

            foreach (var part in pathParts)
            {
                if (currentNode == null)
                    break;

                currentNode.IsExpanded = true;

                var nextNode = currentNode.ChildrenNodes.FirstOrDefault(n => n.FileName?.Contains(part) == true);
                if (nextNode == null)
                    break;

                currentNode = nextNode;
            }
        }

        private static bool IsValidTranslation(string original, string translated)
        {
            static int CountBracketDifference(ReadOnlySpan<char> text)
            {
                int diff = 0;
                foreach (char c in text)
                {
                    if (c == '[') diff++;
                    if (c == ']') diff--;
                }
                return diff;
            }

            return CountBracketDifference(original) == CountBracketDifference(translated);
        }  

        private static Task UpdateUIAsync(Action action)
        {
            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }
    }
}