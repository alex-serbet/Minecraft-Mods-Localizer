using MinecraftLocalizer.Models;
using Microsoft.Win32;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Services.Ai;
using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Properties;
using MinecraftLocalizer.Views;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel
    {
        private void SaveTranslation()
        {
            try
            {
                var checkedNodes = TreeNodes.GetCheckedNodes();
                if (SelectedMode?.Type == TranslationModeType.ResourcePack)
                {
                    checkedNodes = checkedNodes.ExpandToLocalizationFiles();
                }

                if (checkedNodes != null && checkedNodes.Count != 0 && SelectedMode != null && SelectedMode.Type != TranslationModeType.NotSelected)
                {
                    if (SelectedMode.Type == TranslationModeType.OneFile)
                    {
                        if (TrySaveOneFileTranslation(checkedNodes, out string? savedPath))
                        {
                            _dialogService.ShowSuccess(Resources.TranslationSavedMessage, savedPath);
                        }
                        return;
                    }

                    if (_archiveService.TrySave(
                        checkedNodes,
                        LocalizationStrings,
                        LocalizationText,
                        SelectedMode.Type,
                        IsRawViewMode,
                        out string? archivePath))
                    {
                        _dialogService.ShowSuccess(Resources.TranslationSavedMessage, archivePath);
                    }
                    else if (SelectedMode.Type == TranslationModeType.Patchouli)
                    {
                        _dialogService.ShowInformation("Patchouli files are saved during translation. Run translation first.");
                    }
                }
                else
                {
                    _dialogService.ShowError("No valid nodes selected or translation mode is not selected.");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Save error: {ex.Message}");
            }
        }

        private void OpenSettings()
        {
            var settingsViewModel = new SettingsViewModel(_dialogService);

            settingsViewModel.SettingsClosed += async isDirChanged =>
            {
                if (isDirChanged)
                {
                    await ClearLocalizationData();
                }
            };

            _dialogService.ShowDialog<SettingsView>(Application.Current.MainWindow, settingsViewModel);
        }

        private async Task RunTranslation()
        {
            if (IsTranslating)
            {
                _gpt4FreeService.LogFeed.AppendLine("Cancellation requested by user...");
                _ctsTranslation.Cancel();
                return;
            }

            _ctsTranslation = new CancellationTokenSource();

            try
            {
                if (UseGpt4Free)
                {
                    if (!await _gpt4FreePrerequisitesService.EnsureRequirementsAsync())
                        return;

                    await EnsureGpt4FreeServiceAsync();

                    if (!await _gpt4FreeService.PerformInstallationAsync())
                        return;

                    _gpt4FreeService.EnsureServerRunning();
                }
                else
                {
                    var apiKey = Settings.Default.DeepSeekApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        _dialogService.ShowError(Resources.DeepSeekApiKeyMissingMessage);
                        return;
                    }
                }

                IsTranslating = true;
                StreamingText = string.Empty;
                _gpt4FreeService.LogFeed.AppendLine("Translation started.");

                lock (_translationUiCacheLock)
                {
                    _translationUiCache.Clear();
                }
                _followTranslationFile = true;
                var translationStore = new LocalizationDocumentStore();

                var translator = new TranslationOrchestrator(
                    translationStore,
                    new Progress<(int current, int total, double percentage)>(tuple => UpdateProgress(tuple.current, tuple.total, tuple.percentage)),
                    chunk => _ = Application.Current?.Dispatcher.InvokeAsync(() => StreamingText = chunk),
                    onLogMessage: message => _gpt4FreeService.LogFeed.AppendLine(message),
                    onDocumentSnapshot: HandleTranslationSnapshot,
                    selectedMode: SelectedMode,
                    useGpt4Free: UseGpt4Free
                );

                var selectedEntryKeys = LocalizationStrings
                    .Where(item => item.IsSelected)
                    .Select(item => $"{(item.ReferencePath ?? item.ID)}\u001F{item.OriginalString}")
                    .ToHashSet();

                var checkedNodes = TreeNodes.GetCheckedNodes();
                if (SelectedMode?.Type == TranslationModeType.ResourcePack)
                {
                    checkedNodes = checkedNodes.ExpandToLocalizationFiles();
                }

                await EnsureDataGridSelectionAsync(checkedNodes);

                bool result = await translator.TranslateSelectedStrings(
                    checkedNodes,
                    SelectedMode?.Type ?? TranslationModeType.NotSelected,
                    _ctsTranslation.Token,
                    false,
                    _currentDataGridFilePath,
                    selectedEntryKeys);

                if (result)
                {
                    try
                    {
                        if (SelectedMode?.Type == TranslationModeType.ResourcePack)
                        {
                            checkedNodes = checkedNodes.ExpandToLocalizationFiles();
                        }
                        if (checkedNodes != null && checkedNodes.Count != 0 && SelectedMode != null && SelectedMode.Type != TranslationModeType.NotSelected)
                        {
                            string message = Resources.TranslationCompletedMessage;
                            if (SelectedMode.Type == TranslationModeType.OneFile)
                            {
                                if (TrySaveOneFileTranslation(checkedNodes, out string? savedPath))
                                {
                                    _dialogService.ShowSuccess(message, savedPath);
                                }
                                else
                                {
                                    _dialogService.ShowSuccess(message);
                                }
                            }
                            else if (_archiveService.TrySave(
                                checkedNodes,
                                LocalizationStrings,
                                LocalizationText,
                                SelectedMode.Type,
                                IsRawViewMode,
                                out string? archivePath))
                            {
                                _dialogService.ShowSuccess(message, archivePath);
                            }
                            else
                            {
                                _dialogService.ShowSuccess(message);
                            }
                        }
                        else
                        {
                            _dialogService.ShowSuccess(Resources.TranslationCompletedMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _dialogService.ShowError($"Перевод завершен, но не удалось сохранить архив: {ex.Message}");
                    }
                }
                else
                {
                    _dialogService.ShowError(Resources.NoCheckedFilesTranslatingMessage);
                }
            }
            catch (OperationCanceledException)
            {
                _gpt4FreeService.LogFeed.AppendLine("Translation canceled.");
                _dialogService.ShowInformation(Resources.TranslationCanceledMessage);
                StreamingText = string.Empty;
            }
            catch (Exception ex)
            {
                _gpt4FreeService.LogFeed.AppendLine($"Translation error: {ex.Message}");
                _dialogService.ShowError($"Translation error: {ex.Message}");
            }
            finally
            {
                _gpt4FreeService.LogFeed.AppendLine("Translation finished.");
                TranslationProgress = Resources.TranslationStatusIdling;
                TreeNodes.RemoveTranslatingState();

                IsTranslating = false;
                _followTranslationFile = false;
                _ctsTranslation.Dispose();
                _ctsTranslation = new CancellationTokenSource();
            }
        }

        private void HandleTranslationSnapshot(string filePath, IReadOnlyList<LocalizationItem> items, string rawContent)
        {
            UpdateCacheFromSnapshot(filePath, items, rawContent);

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                _ = dispatcher.InvokeAsync(() => HandleTranslationSnapshot(filePath, items, rawContent));
                return;
            }

            bool shouldSwitch = IsTranslating && _followTranslationFile;
            bool isCurrent = string.Equals(filePath, _currentDataGridFilePath, StringComparison.OrdinalIgnoreCase);

            if (!isCurrent && !shouldSwitch)
            {
                return;
            }

            if (shouldSwitch)
            {
                ExpandTreeToFile(filePath);
            }

            LocalizationSnapshot? snapshot;
            lock (_translationUiCacheLock)
            {
                _translationUiCache.TryGetValue(filePath, out snapshot);
            }

            if (snapshot == null)
            {
                return;
            }

            dispatcher ??= Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            _ = dispatcher.InvokeAsync(async () =>
            {
                await _localizationDocumentStore.LoadFromCacheAsync(snapshot.Items, snapshot.RawContent);
                _currentDataGridFilePath = filePath;
            });
        }

        private void ExpandTreeToFile(string filePath)
        {
            var node = FindNodeByFilePath(TreeNodes, filePath);
            if (node == null)
            {
                return;
            }

            var current = node;
            while (current != null)
            {
                current.IsExpanded = true;
                current = current.Parent;
            }
        }

        private static TreeNodeItem? FindNodeByFilePath(IEnumerable<TreeNodeItem> nodes, string filePath)
        {
            string target = NormalizePath(filePath);

            foreach (var node in nodes)
            {
                var found = FindNodeByFilePath(node, target);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static TreeNodeItem? FindNodeByFilePath(TreeNodeItem node, string targetPath)
        {
            if (!string.IsNullOrWhiteSpace(node.FilePath) && NormalizePath(node.FilePath) == targetPath)
            {
                return node;
            }

            foreach (var child in node.ChildrenNodes)
            {
                var found = FindNodeByFilePath(child, targetPath);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string NormalizePath(string path) =>
            path.Replace('\\', '/').Trim().Trim('/');

        private async Task EnsureDataGridSelectionAsync(List<TreeNodeItem> checkedNodes)
        {
            if (!string.IsNullOrWhiteSpace(_currentDataGridFilePath) || checkedNodes.Count == 0)
            {
                return;
            }

            var firstNode = FindFirstLocalizationFileNode(checkedNodes);
            if (firstNode == null)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await OnTreeViewItemSelectedAsync(firstNode);
            });
        }

        private static TreeNodeItem? FindFirstLocalizationFileNode(IEnumerable<TreeNodeItem> nodes)
        {
            foreach (var node in nodes)
            {
                var found = FindFirstLocalizationFileNode(node);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static TreeNodeItem? FindFirstLocalizationFileNode(TreeNodeItem node)
        {
            if (!string.IsNullOrWhiteSpace(node.FilePath) && IsLocalizationFilePath(node.FilePath))
            {
                return node;
            }

            foreach (var child in node.ChildrenNodes)
            {
                var found = FindFirstLocalizationFileNode(child);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool IsLocalizationFilePath(string filePath) =>
            filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".lang", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith(".snbt", StringComparison.OrdinalIgnoreCase);

        private async Task EnsureGpt4FreeServiceAsync()
        {
            try
            {
                _ = _gpt4FreeService.LogFeed;
            }
            catch (ObjectDisposedException)
            {
                try
                {
                    _gpt4FreeService.LogFeed.PropertyChanged -= OnConsoleOutputChanged;
                }
                catch (ObjectDisposedException)
                {
                }

                _gpt4FreeService = new Gpt4FreeService(_dialogService);
                _gpt4FreeService.LogFeed.PropertyChanged += OnConsoleOutputChanged;
                await Task.Delay(100);
            }
        }

        private void OpenDirectory()
        {
            string directoryPath = Settings.Default.DirectoryPath;

            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Process.Start("explorer.exe", directoryPath);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError($"Error opening the folder: {ex.Message}");
                }
            }
            else
            {
                _dialogService.ShowError("Folder not found.");
            }
        }

        private void OpenNodeInExplorer(TreeNodeItem? node)
        {
            node ??= SelectedTreeNode;
            if (node == null)
            {
                _dialogService.ShowError("No node selected.");
                return;
            }

            string? existingPath = GetExistingPath(node);
            if (string.IsNullOrWhiteSpace(existingPath))
            {
                _dialogService.ShowError("Path not found.");
                return;
            }

            try
            {
                if (Directory.Exists(existingPath))
                {
                    Process.Start("explorer.exe", existingPath);
                    return;
                }

                if (File.Exists(existingPath))
                {
                    Process.Start("explorer.exe", $"/select,\"{existingPath}\"");
                    return;
                }

                _dialogService.ShowError("Path not found.");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error opening the folder: {ex.Message}");
            }
        }

        private static string? GetExistingPath(TreeNodeItem node)
        {
            if (!string.IsNullOrWhiteSpace(node.FilePath) &&
                (File.Exists(node.FilePath) || Directory.Exists(node.FilePath)))
            {
                return node.FilePath;
            }

            if (!string.IsNullOrWhiteSpace(node.ModPath) &&
                (File.Exists(node.ModPath) || Directory.Exists(node.ModPath)))
            {
                return node.ModPath;
            }

            return node.Parent != null ? GetExistingPath(node.Parent) : null;
        }

        private void CopySelectedCell(DataGrid? grid)
        {
            if (grid == null)
            {
                _dialogService.ShowError("No grid available.");
                return;
            }

            var cell = grid.CurrentCell;
            if (cell.Column == null)
            {
                if (grid.SelectedCells.Count > 0)
                {
                    cell = grid.SelectedCells[0];
                }
                else
                {
                    _dialogService.ShowError("No cell selected.");
                    return;
                }
            }

            if (cell.Item is not LocalizationItem item || cell.Column == null)
            {
                _dialogService.ShowError("No cell selected.");
                return;
            }

            string? text = GetCellText(grid, cell.Column, item);
            if (string.IsNullOrEmpty(text))
            {
                _dialogService.ShowError("No cell text available.");
                return;
            }

            Clipboard.SetText(text);
        }

        private static string? GetCellText(DataGrid grid, DataGridColumn column, LocalizationItem item)
        {
            if (column.GetCellContent(item) is FrameworkElement element)
            {
                switch (element)
                {
                    case TextBlock textBlock:
                        return textBlock.Text;
                    case TextBox textBox:
                        return textBox.Text;
                    case RichTextBox richTextBox:
                    {
                        var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                        return range.Text.TrimEnd('\r', '\n');
                    }
                }
            }

            if (column is DataGridTextColumn textColumn &&
                textColumn.Binding is Binding binding &&
                !string.IsNullOrWhiteSpace(binding.Path?.Path))
            {
                return binding.Path.Path switch
                {
                    nameof(LocalizationItem.ID) => item.ID,
                    nameof(LocalizationItem.OriginalString) => item.OriginalString,
                    nameof(LocalizationItem.TranslatedString) => item.TranslatedString,
                    _ => null
                };
            }

            string header = column.Header?.ToString() ?? string.Empty;
            if (header.Equals("ID", StringComparison.OrdinalIgnoreCase))
                return item.ID;

            if (header.Equals(Resources.OriginalText, StringComparison.Ordinal))
                return item.OriginalString;

            if (header.Equals(Resources.TranslatedText, StringComparison.Ordinal))
                return item.TranslatedString;

            return column.DisplayIndex switch
            {
                1 => item.ID,
                2 => item.OriginalString,
                3 => item.TranslatedString,
                _ => null
            };
        }

        private bool TrySaveOneFileTranslation(List<TreeNodeItem> checkedNodes, out string? savedPath)
        {
            savedPath = null;

            var node = checkedNodes.FirstOrDefault();
            if (node == null)
            {
                _dialogService.ShowError(Resources.NoCheckedFilesSavingMessage);
                return false;
            }

            string? fileExtension = LocalizationArchiveWriter.TryGetOneFileExtension(node);
            string defaultExtension = string.IsNullOrWhiteSpace(fileExtension) ? "json" : fileExtension;

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files|*.json|LANG files|*.lang|SNBT files|*.snbt|All files (*.*)|*.*",
                DefaultExt = $".{defaultExtension}",
                AddExtension = true,
                Title = "Save translated file"
            };

            if (!string.IsNullOrWhiteSpace(node.FileName))
            {
                string baseName = Path.GetFileNameWithoutExtension(node.FileName);
                saveFileDialog.FileName = $"{baseName}.{defaultExtension}";
            }

            bool? result = saveFileDialog.ShowDialog();
            if (result != true)
            {
                return false;
            }

            string outputPath = saveFileDialog.FileName;
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return false;
            }

            savedPath = LocalizationArchiveWriter.SaveSingleFileToPath(
                node,
                LocalizationStrings,
                LocalizationText,
                outputPath,
                IsRawViewMode);

            return true;
        }
    }
}










