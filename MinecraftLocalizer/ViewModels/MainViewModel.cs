﻿using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Models.Services;
using MinecraftLocalizer.Models.Utils;
using MinecraftLocalizer.Views;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;


namespace MinecraftLocalizer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly LocalizationStringManager _localizationStringManager;
        private readonly QuestsService _questsService;
        private readonly ModsService _modsService;
        private readonly PatchouliService _patchouliService;
        private readonly BetterQuestingService _betterQuestingService;
        private readonly Gpt4FreeService _gpt4FreeService;

        private DateTime _lastProgressUpdate = DateTime.MinValue;
        private CancellationTokenSource _cts = new();

        public MainViewModel()
        {
            _localizationStringManager = new LocalizationStringManager();
            _questsService = new QuestsService();
            _modsService = new ModsService();
            _patchouliService = new PatchouliService();
            _betterQuestingService = new BetterQuestingService();
            _gpt4FreeService = new Gpt4FreeService();

            InitializeCollections();
            InitializeCommands();
            InitializeModes();
        }

        #region Properties

        public string TranslationButtonText => IsTranslating ? Properties.Resources.TranslationCancel : Properties.Resources.RunTranslation;
        public ObservableCollection<LocalizationItem> LocalizationStrings => _localizationStringManager.LocalizationStrings;
        public ObservableCollection<TreeNodeItem> TreeNodes { get; } = [];
        public ObservableCollection<TranslationModeItem> Modes { get; } = [];
        public ICollectionView? DataGridCollectionView { get; private set; }
        public ICollectionView? TreeNodesCollectionView { get; private set; }


        private TranslationModeItem? _selectedMode;
        public TranslationModeItem? SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value, () => _ = LoadDataTreeViewAsync());
        }


        private string _searchDataGridText = string.Empty;
        public string SearchDataGridText
        {
            get => _searchDataGridText;
            set => SetProperty(ref _searchDataGridText, value, RefreshDataGridSearch);
        }


        private string _searchTreeViewText = string.Empty;
        public string SearchTreeViewText
        {
            get => _searchTreeViewText;
            set => SetProperty(ref _searchTreeViewText, value, RefreshTreeViewSearch);
        }


        private string _translationProgress = Properties.Resources.TranslationStatusIdling;
        public string TranslationProgress
        {
            get => _translationProgress;
            set => SetProperty(ref _translationProgress, value);
        }


        private bool _isAllSelected = true;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged();
                    SelectAllItems(value);
                }
            }
        }


        private bool _isTreeNodesLogoVisible = true;
        public bool IsTreeNodesLogoVisible
        {
            get => _isTreeNodesLogoVisible;
            private set
            {
                if (_isTreeNodesLogoVisible != value)
                {
                    _isTreeNodesLogoVisible = value;
                    OnPropertyChanged();
                }
            }
        }


        private bool _isDataGridLogoVisible = true;
        public bool IsDataGridLogoVisible
        {
            get => _isDataGridLogoVisible;
            private set
            {
                if (_isDataGridLogoVisible != value)
                {
                    _isDataGridLogoVisible = value;
                    OnPropertyChanged();
                }
            }
        }


        private bool _isTranslating;
        public bool IsTranslating
        {
            get => _isTranslating;
            set => SetProperty(ref _isTranslating, value, () =>
            {
                OnPropertyChanged(nameof(TranslationButtonText));
                OnPropertyChanged(nameof(IsTranslationInProgress));
            });
        }


        public bool IsTranslationInProgress => IsTranslating;


        #endregion

        #region Commands

        public ICommand? SaveTranslationCommand { get; private set; }
        public ICommand? RunTranslationCommand { get; private set; }
        public ICommand? OpenSettingsCommand { get; private set; }
        public ICommand? OpenDirectoryCommand { get; private set; }
        public ICommand? OnTreeViewItemSelectedCommand { get; private set; }
        public ICommand? OnApplicationExitCommand { get; private set; }

        #endregion

        #region Initialization

        private void InitializeCollections()
        {
            DataGridCollectionView = CollectionViewSource.GetDefaultView(LocalizationStrings);
            DataGridCollectionView.Filter = FilterDataGridEntries;
            LocalizationStrings.CollectionChanged += HandleCollectionChanged;

            TreeNodesCollectionView = CollectionViewSource.GetDefaultView(TreeNodes);
            TreeNodesCollectionView.Filter = FilterTreeViewEntries;

            TreeNodes.CollectionChanged += (s, e) => UpdateTreeNodesLogoVisibility();
            LocalizationStrings.CollectionChanged += (s, e) => UpdateDataGridLogoVisibility();
        }

        private void InitializeCommands()
        {
            SaveTranslationCommand = new RelayCommand(SaveTranslation);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            RunTranslationCommand = new RelayCommand(async () => await RunTranslation());
            OpenDirectoryCommand = new RelayCommand(OpenDirectory);
            OnTreeViewItemSelectedCommand = new RelayCommand<TreeNodeItem>(async node => await OnTreeViewItemSelectedAsync(node));
            OnApplicationExitCommand = new RelayCommand(OnApplicationExit);
        }

        private void InitializeModes()
        {
            Modes.AddRange(
            [
                new TranslationModeItem { ModeTitle = Properties.Resources.NotSelectedModeTitle, Type = TranslationModeType.NotSelected },
                new TranslationModeItem { ModeTitle = Properties.Resources.ModsModeTitle, Type = TranslationModeType.Mods },
                new TranslationModeItem { ModeTitle = "FTB Quests", Type = TranslationModeType.Quests },
                new TranslationModeItem { ModeTitle = "Patchouli", Type = TranslationModeType.Patchouli },
                new TranslationModeItem { ModeTitle = "Better Questing", Type = TranslationModeType.BetterQuesting },
            ]);

            SelectedMode = Modes.FirstOrDefault();
        }

        #endregion

        #region Private Helpers

        private void RefreshDataGridSearch() => DebounceHelper.Debounce(() => Application.Current.Dispatcher.Invoke(() => DataGridCollectionView?.Refresh()));
        private void RefreshTreeViewSearch() => DebounceHelper.Debounce(() => Application.Current.Dispatcher.Invoke(() => TreeNodesCollectionView?.Refresh()));

        private bool FilterDataGridEntries(object item) => item is LocalizationItem entry && (entry.OriginalString?.Contains(SearchDataGridText, StringComparison.CurrentCultureIgnoreCase) == true || entry.TranslatedString?.Contains(SearchDataGridText, StringComparison.CurrentCultureIgnoreCase) == true);
        private bool FilterTreeViewEntries(object item) => item is TreeNodeItem node && (node.FileName.Contains(SearchTreeViewText, StringComparison.CurrentCultureIgnoreCase) || node.ChildrenNodes.Any(child => child.FileName.Contains(SearchTreeViewText, StringComparison.CurrentCultureIgnoreCase)));

        private void SaveTranslation()
        {
            try
            {
                if (SelectedMode != null && SelectedMode.Type != TranslationModeType.NotSelected)
                {
                    LocalizationSaveManager.SaveTranslation(TreeNodes.GetCheckedNodes(), LocalizationStrings, SelectedMode.Type);
                    DialogService.ShowSuccess(Properties.Resources.TranslationSavedMessage);
                }
            }
            catch (Exception ex)
            {
                DialogService.ShowError($"Save error: {ex.Message}");
            }
        }
        private void OpenSettings()
        {
            DialogService.ShowDialog<SettingsView>(Application.Current.MainWindow);
        }
        private async Task RunTranslation()
        {
            if (IsTranslating)
            {
                _cts.Cancel();
                return;
            }

            _cts = new CancellationTokenSource();

            try
            {
                if (!await _gpt4FreeService.IsGpt4FreeExistAsync())
                    return;

                IsTranslating = true;

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var translator = new TranslationManager(
                    _localizationStringManager,
                    new Progress<(int current, int total, double percentage)>(tuple => UpdateProgress(tuple.current, tuple.total, tuple.percentage))
                );

                bool result = await translator.TranslateSelectedStrings(
                    TreeNodes.GetCheckedNodes(),
                    SelectedMode?.Type ?? TranslationModeType.NotSelected,
                    _cts.Token);

                if (result)
                {
                    DialogService.ShowSuccess(Properties.Resources.TranslationCompletedMessage);
                }
                else
                    DialogService.ShowError(Properties.Resources.NoCheckedFilesTranslatingMessage);
            }
            catch (OperationCanceledException)
            {
                DialogService.ShowInformation(Properties.Resources.TranslationCanceledMessage);
            }
            finally
            {
                TranslationProgress = TranslationProgress = Properties.Resources.TranslationStatusIdling;
                TreeNodes.RemoveTranslatingState();

                IsTranslating = false;
                _cts.Dispose();
                _cts = new CancellationTokenSource();
            }
        }
        private void OpenDirectory()
        {
            string directoryPath = Properties.Settings.Default.DirectoryPath;

            if (Directory.Exists(directoryPath))
            {
                try
                {
                    Process.Start("explorer.exe", directoryPath);
                }
                catch (Exception ex)
                {
                    DialogService.ShowError($"Error opening the folder: {ex.Message}");
                }
            }
            else
            {
                DialogService.ShowError("Folder not found.");
            }
        }
        private async Task OnTreeViewItemSelectedAsync(TreeNodeItem? node)
        {
            if (node is null || node.IsRoot || !(node.FilePath.EndsWith(".json") || node.FilePath.EndsWith(".lang") || node.FilePath.EndsWith(".snbt")))
                return;

            if (SelectedMode != null)
            {
                await _localizationStringManager.LoadStringsAsync(node, SelectedMode.Type);
                DataGridCollectionView?.Refresh();
            }
        }
        private async Task LoadDataTreeViewAsync()
        {
            if (SelectedMode == null) return;

            TreeNodes.Clear();
            LocalizationStrings.Clear();
            SearchTreeViewText = string.Empty;
            SearchDataGridText = string.Empty;

            IEnumerable<TreeNodeItem> nodes = SelectedMode.Type switch
            {
                TranslationModeType.Quests => await _questsService.LoadQuestsNodesAsync(),
                TranslationModeType.Mods => await _modsService.LoadModsNodesAsync(),
                TranslationModeType.Patchouli => await _patchouliService.LoadPatchouliNodesAsync(),
                TranslationModeType.BetterQuesting => await _betterQuestingService.LoadQuestsNodesAsync(),
                _ => []
            };

            if (nodes.Any())
            {
                TreeNodes.AddRange(nodes);
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedMode = Modes.FirstOrDefault();
                });
            }
        }
        private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DataGridCollectionView?.Refresh();
        }
        private void UpdateProgress(int current, int total, double percentage)
        {
            if ((DateTime.Now - _lastProgressUpdate).TotalMilliseconds < 300)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                TranslationProgress = string.Format(
                    Properties.Resources.TranslationStatusRunning,
                    current,
                    total,
                    percentage
                );
            });

            _lastProgressUpdate = DateTime.Now;
        }
        private void UpdateTreeNodesLogoVisibility()
        {
            IsTreeNodesLogoVisible = TreeNodes.Count == 0;
        }
        private void UpdateDataGridLogoVisibility()
        {
            IsDataGridLogoVisible = LocalizationStrings.Count == 0;
        }
        private void SelectAllItems(bool isSelected)
        {
            if (DataGridCollectionView != null)
                foreach (LocalizationItem item in DataGridCollectionView)
                {
                    item.IsSelected = isSelected;
                }
        }
        private void OnApplicationExit()
        {
            Gpt4FreeService.KillGpt4FreeProcess();
        }

        #endregion
    }
}