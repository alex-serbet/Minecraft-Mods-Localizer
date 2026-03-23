using MinecraftLocalizer.Models;
using MinecraftLocalizer.ViewModels;

namespace MinecraftLocalizer.Models.Localization
{
    public partial class TranslationOrchestrator
    {
        private static List<(TreeNodeItem Source, TreeNodeItem Target)> CollectTargetNodes(
            List<TreeNodeItem> nodes,
            string sourceLanguage,
            TranslationModeType modeType)
        {
            var pairs = new List<(TreeNodeItem, TreeNodeItem)>();
            var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                if (modeType == TranslationModeType.Patchouli)
                {
                    var sourceFolder = GetPatchouliSourceFolder(node, sourceLanguage);

                    if (IsLocalizationFile(node.FileName))
                    {
                        if (node.FilePath != null && seenTargets.Add(node.FilePath))
                        {
                            pairs.Add((sourceFolder ?? node, node));
                        }

                        continue;
                    }

                    if (sourceFolder == null)
                    {
                        ShowMissingLanguageFileError(sourceLanguage, node.FileName);
                        continue;
                    }

                    var localizationFiles = sourceFolder.ChildrenNodes
                        .Where(n => n.FileName != null && IsLocalizationFile(n.FileName))
                        .ToList();

                    foreach (var file in localizationFiles)
                    {
                        if (file.FilePath == null || !seenTargets.Add(file.FilePath))
                            continue;

                        pairs.Add((sourceFolder, file));
                    }

                    continue;
                }

                var targetNodes = GetTargetNodes(node, sourceLanguage);

                if (targetNodes.Count == 0 && IsLocalizationFile(node.FileName))
                {
                    targetNodes.Add(node);
                }
                else if (targetNodes.Count == 0)
                {
                    ShowMissingLanguageFileError(sourceLanguage, node.FileName);
                    continue;
                }

                if (modeType == TranslationModeType.Patchouli)
                {
                    var languageFolders = node.ChildrenNodes
                        .Where(n => n.FileName != null && !IsLocalizationFile(n.FileName))
                        .ToList();

                    var enUsFolder = languageFolders.FirstOrDefault(folder =>
                        folder.FileName != null && folder.FileName.Equals("en_us", StringComparison.OrdinalIgnoreCase));

                    TreeNodeItem? selectedFolder = enUsFolder;
                    if (selectedFolder == null && languageFolders.Count != 0)
                    {
                        selectedFolder = languageFolders.First();
                    }

                    if (selectedFolder != null)
                    {
                        var localizationFiles = selectedFolder.ChildrenNodes
                            .Where(n => n.FileName != null && IsLocalizationFile(n.FileName))
                            .ToList();

                        foreach (var file in localizationFiles)
                        {
                            if (file.FilePath == null || !seenTargets.Add(file.FilePath))
                                continue;

                            pairs.Add((node, file));
                        }
                    }
                    else if (targetNodes.Count != 0)
                    {
                        var target = targetNodes.First();
                        if (target.FilePath != null && seenTargets.Add(target.FilePath))
                        {
                            pairs.Add((node, target));
                        }
                    }
                }
                else if (targetNodes.Count != 0)
                {
                    var target = targetNodes.First();
                    if (target.FilePath != null && seenTargets.Add(target.FilePath))
                    {
                        pairs.Add((node, target));
                    }
                }
            }

            return pairs;
        }

        private static void ShowMissingLanguageFileError(string language, string fileName)
        {
            LocalizationDialogContext.DialogService.ShowError(string.Format(
                Properties.Resources.SourceLanguageFileMissingMessage,
                language,
                fileName));
        }

        public static List<TreeNodeItem> GetTargetNodes(TreeNodeItem node, string sourceLanguage)
        {
            var targetNodes = node.ChildrenNodes
                .Where(n => n.FileName != null &&
                            IsLocalizationFile(n.FileName) &&
                            IsEnUsLocaleFile(n.FileName))
                .ToList();

            if (targetNodes.Count == 0)
            {
                targetNodes = node.ChildrenNodes
                    .Where(n => n.FileName != null &&
                                IsLocalizationFile(n.FileName) &&
                                IsMatchingLanguageFile(n.FileName, sourceLanguage))
                    .ToList();
            }

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
                    targetNodes = [.. languageFolder.ChildrenNodes
                        .Where(n => n.FileName != null && IsLocalizationFile(n.FileName))];
                }
            }

            return targetNodes;
        }

        private static TreeNodeItem GetRootNode(TreeNodeItem node)
        {
            var current = node;
            while (current.Parent != null && !current.IsRoot)
            {
                current = current.Parent;
            }

            return current;
        }

        private static TreeNodeItem? GetPatchouliSourceFolder(TreeNodeItem node, string sourceLanguage)
        {
            if (IsPatchouliLanguageFolder(node))
                return node;

            if (IsLocalizationFile(node.FileName))
            {
                if (node.Parent != null && IsPatchouliLanguageFolder(node.Parent))
                    return node.Parent;

                return null;
            }

            var rootNode = GetRootNode(node);
            return FindPatchouliLanguageFolder(rootNode, sourceLanguage);
        }

        private static bool IsPatchouliLanguageFolder(TreeNodeItem node)
        {
            return node.ChildrenNodes.Count > 0 &&
                   node.ChildrenNodes.All(n => n.FileName != null && IsLocalizationFile(n.FileName));
        }

        private static TreeNodeItem? FindPatchouliLanguageFolder(
            TreeNodeItem rootNode,
            string sourceLanguage)
        {
            var languageFolders = rootNode.ChildrenNodes
                .Where(n => n.FileName != null && !IsLocalizationFile(n.FileName))
                .ToList();

            var sourceFolder = languageFolders.FirstOrDefault(folder =>
                folder.FileName != null &&
                folder.FileName.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase));

            if (sourceFolder != null)
                return sourceFolder;

            var enUsFolder = languageFolders.FirstOrDefault(folder =>
                folder.FileName != null &&
                folder.FileName.Equals("en_us", StringComparison.OrdinalIgnoreCase));

            return enUsFolder ?? languageFolders.FirstOrDefault();
        }

        private async Task<int> CalculateTotalStringsAsync(
            List<(TreeNodeItem Source, TreeNodeItem Target)> pairs,
            TranslationModeType modeType,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            int total = 0;
            bool allowSelectAll = selectedEntryKeys == null || selectedEntryKeys.Count == 0;
            foreach (var (source, target) in pairs)
            {
                await LoadStringsAsync(source, target);
                if (modeType == TranslationModeType.Patchouli)
                    ApplyPatchouliSelectionByJsonReference();

                ApplySelectionForCurrentFile(target.FilePath, currentDataGridFilePath, selectedEntryKeys);
                EnsureSelectionExistsOrSelectAll(modeType, allowSelectAll);
                total += _localizationManager.LocalizationStrings.Count(e => e.IsSelected);
            }

            return total;
        }

        private async Task SaveTranslations(List<TreeNodeItem> nodes, TranslationModeType modeType)
        {
            await Task.Run(() =>
            {
                LocalizationArchiveWriter.SaveTranslation(
                    nodes,
                    _localizationManager.LocalizationStrings,
                    _localizationManager.RawContent,
                    modeType,
                    isRawViewMode: true);
            });
        }

        private async Task LoadStringsAsync(TreeNodeItem sourceNode, TreeNodeItem targetNode)
        {
            var cacheKey = $"{sourceNode.FilePath}|{targetNode.FilePath}";
            var loadSource = LoadSourceCache.GetOrAdd(cacheKey, _ =>
                CreateLoadSource(sourceNode, targetNode));

            await _localizationManager.LoadStringsAsync(loadSource);
        }

        private void ApplySelectionForCurrentFile(
            string? targetFilePath,
            string? currentDataGridFilePath,
            HashSet<string>? selectedEntryKeys)
        {
            if (_selectedMode?.Type == TranslationModeType.Patchouli)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetFilePath) ||
                string.IsNullOrWhiteSpace(currentDataGridFilePath) ||
                selectedEntryKeys is null ||
                !AreSameFilePath(targetFilePath, currentDataGridFilePath))
            {
                return;
            }

            foreach (var item in _localizationManager.LocalizationStrings)
            {
                string key = BuildEntryKey(item);
                item.IsSelected = selectedEntryKeys.Contains(key);
            }
        }

        private void EnsureSelectionExistsOrSelectAll(TranslationModeType modeType)
        {
            if (_localizationManager.LocalizationStrings.Any(e => e.IsSelected))
                return;

            if (modeType == TranslationModeType.Patchouli)
            {
                ApplyPatchouliSelectionByJsonReference();
                return;
            }

            foreach (var item in _localizationManager.LocalizationStrings)
            {
                item.IsSelected = true;
            }
        }

        private void EnsureSelectionExistsOrSelectAll(TranslationModeType modeType, bool allowSelectAll)
        {
            if (!allowSelectAll)
                return;

            EnsureSelectionExistsOrSelectAll(modeType);
        }

        private static string BuildEntryKey(LocalizationItem item) =>
            $"{(item.ReferencePath ?? item.ID)}\u001F{item.OriginalString}";

        private void ApplyPatchouliSelectionByJsonReference()
        {
            foreach (var item in _localizationManager.LocalizationStrings)
            {
                bool isPatchouliKey = IsPatchouliKey(item.ReferencePath ?? item.ID);
                bool looksLikeKey = LooksLikeLocalizationKey(item.OriginalString);
                item.IsSelected = isPatchouliKey && !looksLikeKey;
            }
        }

        private bool IsPatchouliKey(string? jsonReference)
        {
            if (string.IsNullOrWhiteSpace(jsonReference))
                return false;

            string key = jsonReference;
            if (jsonReference.StartsWith("#/", StringComparison.Ordinal))
            {
                int slashIndex = jsonReference.LastIndexOf('/');
                key = slashIndex >= 0 ? jsonReference[(slashIndex + 1)..] : jsonReference;
            }

            key = key.Replace("~1", "/", StringComparison.Ordinal)
                     .Replace("~0", "~", StringComparison.Ordinal);

            return _patchouliKeys.Contains(key);
        }

        private static bool LooksLikeLocalizationKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            if (text.Length < 6)
                return false;

            if (text.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
                return false;

            if (!text.Contains('.', StringComparison.Ordinal))
                return false;

            while (text.EndsWith(".", StringComparison.Ordinal))
            {
                text = text[..^1];
            }

            var parts = text.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return false;

            foreach (var part in parts)
            {
                foreach (char c in part)
                {
                    if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                        return false;
                }
            }

            return true;
        }
    }
}







