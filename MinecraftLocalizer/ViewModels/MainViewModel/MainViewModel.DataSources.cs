using Microsoft.Win32;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Utils;
using System.IO;
using System.Windows;

namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel
    {
        public async Task OpenFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "JSON, LANG, SNBT files|*.json;*.lang;*.snbt|All files (*.*)|*.*"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                await ClearLocalizationData(keepMode: true);

                string filePath = openFileDialog.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var nodes = await _fileService.LoadFileNodesAsync(filePath);

                    if (nodes.Any())
                    {
                        foreach (var node in nodes)
                        {
                            TreeNodes.Add(node);
                        }

                        TreeNodesCollectionView?.Refresh();

                        var oneFileMode = Modes.FirstOrDefault(m => m.Type == TranslationModeType.OneFile);
                        if (oneFileMode != null)
                        {
                            SetSelectedModeSilently(oneFileMode);
                        }

                        var firstNode = nodes.FirstOrDefault();
                        if (firstNode != null)
                        {
                            firstNode.IsChecked = true;
                            await OnTreeViewItemSelectedAsync(firstNode);
                        }
                    }
                }
            }
        }

        public async Task OpenResourcePack()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "ZIP files|*.zip|All files (*.*)|*.*"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string archivePath = openFileDialog.FileName;
                if (!string.IsNullOrWhiteSpace(archivePath))
                {
                    var resourcePackMode = Modes.FirstOrDefault(m => m.Type == TranslationModeType.ResourcePack);
                    if (resourcePackMode != null)
                    {
                        SetSelectedModeSilently(resourcePackMode);
                    }

                    await ClearLocalizationData(keepMode: true);
                    await LoadResourcePackNodesAsync(archivePath);
                }
            }
        }

        public async Task ClearLocalizationData()
        {
            await ClearLocalizationData(keepMode: false);
        }

        public async Task ClearLocalizationData(bool keepMode)
        {
            if (IsTranslating)
            {
                _ctsTranslation.Cancel();
            }

            TreeNodes.Clear();
            LocalizationStrings.Clear();
            lock (_translationUiCacheLock)
            {
                _translationUiCache.Clear();
            }

            if (!keepMode)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedMode = Modes.FirstOrDefault();
                });
            }
        }

        private async Task OnTreeViewItemSelectedAsync(TreeNodeItem? node)
        {
            SelectedTreeNode = node;

            if (node is null || (node.IsRoot && node.HasItems) || !IsLocalizationFile(node.FilePath))
                return;

            if (SelectedMode is null)
                return;

            if (IsTranslating)
            {
                _followTranslationFile = false;
                SnapshotCurrentDocument();
                if (await TryRestoreFromCacheAsync(node))
                {
                    return;
                }
            }

            ILoadSource? source = node.ModPath switch
            {
                string mod when mod.EndsWith(".zip") => new ZipLoadSource(mod, node.FilePath),
                string mod when mod.EndsWith(".jar") => new JarLoadSource(mod, node.FilePath),
                _ => new FileLoadSource(node.FilePath)
            };

            await _localizationDocumentStore.LoadStringsAsync(source);
            _currentDataGridFilePath = node.FilePath;
        }

        private void SnapshotCurrentDocument()
        {
            if (string.IsNullOrWhiteSpace(_currentDataGridFilePath))
            {
                return;
            }

            var snapshot = new LocalizationSnapshot
            {
                Items = CloneItems(_localizationDocumentStore.LocalizationStrings),
                RawContent = _localizationDocumentStore.RawContent
            };

            lock (_translationUiCacheLock)
            {
                _translationUiCache[_currentDataGridFilePath] = snapshot;
            }
        }

        private async Task<bool> TryRestoreFromCacheAsync(TreeNodeItem node)
        {
            if (string.IsNullOrWhiteSpace(node.FilePath))
            {
                return false;
            }

            LocalizationSnapshot? snapshot;
            lock (_translationUiCacheLock)
            {
                _translationUiCache.TryGetValue(node.FilePath, out snapshot);
            }

            if (snapshot != null)
            {
                await _localizationDocumentStore.LoadFromCacheAsync(snapshot.Items, snapshot.RawContent);
                _currentDataGridFilePath = node.FilePath;
                return true;
            }

            return false;
        }

        private void UpdateCacheFromSnapshot(string filePath, IReadOnlyList<LocalizationItem> items, string rawContent)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var snapshot = new LocalizationSnapshot
            {
                Items = CloneItems(items),
                RawContent = rawContent
            };

            lock (_translationUiCacheLock)
            {
                _translationUiCache[filePath] = snapshot;
            }
        }

        private static List<LocalizationItem> CloneItems(IEnumerable<LocalizationItem> items)
        {
            var clone = new List<LocalizationItem>();
            foreach (var item in items)
            {
                clone.Add(new LocalizationItem
                {
                    DataType = item.DataType,
                    IsSelected = item.IsSelected,
                    RowNumber = item.RowNumber,
                    ID = item.ID,
                    ReferencePath = item.ReferencePath,
                    OriginalString = item.OriginalString,
                    TranslatedString = item.TranslatedString
                });
            }

            return clone;
        }

        private static bool IsLocalizationFile(string filePath) =>
            filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);

        private async Task OnComboBoxItemSelectedAsync()
        {
            if (SelectedMode == null) return;

            StreamingText = string.Empty;

            if (SelectedMode.Type == TranslationModeType.OneFile)
            {
                if (TreeNodes.Any())
                {
                    TreeNodes.Clear();
                    LocalizationStrings.Clear();
                    SearchTreeViewText = string.Empty;
                    SearchDataGridText = string.Empty;
                }

                await OpenFileDialogForOneFileMode();
                return;
            }

            if (SelectedMode.Type == TranslationModeType.ResourcePack)
            {
                if (TreeNodes.Any())
                {
                    TreeNodes.Clear();
                    LocalizationStrings.Clear();
                    SearchTreeViewText = string.Empty;
                    SearchDataGridText = string.Empty;
                }

                await OpenFileDialogForResourcePackMode();
                return;
            }

            TreeNodes.Clear();
            LocalizationStrings.Clear();
            SearchTreeViewText = string.Empty;
            SearchDataGridText = string.Empty;

            IEnumerable<TreeNodeItem> nodes = await _modeNodeLoader.LoadAsync(SelectedMode.Type);

            if (nodes.Any())
            {
                TreeNodes.AddRange(nodes);
            }
            else
            {
                await ClearLocalizationData();
            }
        }

        private async Task OpenFileDialogForOneFileMode()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "JSON, LANG, SNBT files|*.json;*.lang;*.snbt|All files (*.*)|*.*",
                Title = "Select a localization file for One File mode"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string filePath = openFileDialog.FileName;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    var nodes = await _fileService.LoadFileNodesAsync(filePath);

                    if (nodes.Any())
                    {
                        foreach (var node in nodes)
                        {
                            TreeNodes.Add(node);
                        }

                        TreeNodesCollectionView?.Refresh();

                        var firstNode = nodes.FirstOrDefault();
                        if (firstNode != null)
                        {
                            firstNode.IsChecked = true;
                            await OnTreeViewItemSelectedAsync(firstNode);
                        }
                    }
                }
            }
            else
            {
                var previousMode = Modes.FirstOrDefault(m => m.Type != TranslationModeType.OneFile);
                if (previousMode != null)
                {
                    SetSelectedModeSilently(previousMode);
                }
            }
        }

        private async Task OpenFileDialogForResourcePackMode()
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "ZIP files|*.zip|All files (*.*)|*.*",
                Title = "Select a resource pack archive"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                string archivePath = openFileDialog.FileName;
                if (!string.IsNullOrWhiteSpace(archivePath))
                {
                    await LoadResourcePackNodesAsync(archivePath);
                }
            }
            else
            {
                var previousMode = Modes.FirstOrDefault(m => m.Type != TranslationModeType.ResourcePack);
                if (previousMode != null)
                {
                    SetSelectedModeSilently(previousMode);
                }
            }
        }

        private async Task LoadResourcePackNodesAsync(string archivePath)
        {
            var nodes = await _zipService.LoadZipNodesAsync(archivePath);
            if (nodes.Any())
            {
                TreeNodes.AddRange(nodes);
            }
        }

        private void SelectNodesMissingTargetLocale()
        {
            string targetLanguage = Properties.Settings.Default.TargetLanguage;
            if (string.IsNullOrWhiteSpace(targetLanguage))
            {
                return;
            }

            foreach (var node in TreeNodes)
            {
                if (IsMissingTargetLocale(node, targetLanguage))
                {
                    node.IsChecked = true;
                }
            }
        }

        private static bool IsMissingTargetLocale(TreeNodeItem node, string targetLanguage)
        {
            if (node.HasItems)
            {
                return !ContainsTargetLocale(node, targetLanguage);
            }

            if (!string.IsNullOrWhiteSpace(node.FileName) && IsLocalizationFileName(node.FileName))
            {
                return !IsLocaleFileName(node.FileName, targetLanguage);
            }

            return false;
        }

        private static bool ContainsTargetLocale(TreeNodeItem node, string targetLanguage)
        {
            if (IsLocaleFolder(node, targetLanguage) || IsLocaleFile(node, targetLanguage))
            {
                return true;
            }

            foreach (var child in node.ChildrenNodes)
            {
                if (ContainsTargetLocale(child, targetLanguage))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLocaleFolder(TreeNodeItem node, string targetLanguage)
        {
            return node.HasItems &&
                   !string.IsNullOrWhiteSpace(node.FileName) &&
                   node.FileName.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocaleFile(TreeNodeItem node, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(node.FileName))
            {
                return false;
            }

            return IsLocalizationFileName(node.FileName) &&
                   IsLocaleFileName(node.FileName, targetLanguage);
        }

        private static bool IsLocaleFileName(string fileName, string targetLanguage)
        {
            return fileName.Contains(targetLanguage, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocalizationFileName(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);
        }
    }
}









