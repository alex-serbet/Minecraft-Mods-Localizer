using MinecraftLocalizer.Interfaces.Ai;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Interfaces.Translation;
using MinecraftLocalizer.Models;
using MinecraftLocalizer.Models.Localization;
using MinecraftLocalizer.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;


namespace MinecraftLocalizer.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly LocalizationDocumentStore _localizationDocumentStore;
        private readonly IZipService _zipService;
        private readonly IFileService _fileService;
        private readonly IRequirementsService _gpt4FreePrerequisitesService;
        private readonly ITranslationModeNodeLoader _modeNodeLoader;
        private readonly ITranslationArchiveService _archiveService;
        private readonly IDialogService _dialogService;
        private IGpt4FreeService _gpt4FreeService;

        private DateTime _lastProgressUpdate = DateTime.MinValue;
        private CancellationTokenSource _ctsTranslation = new();
        private string? _currentDataGridFilePath;
        private readonly Dictionary<string, LocalizationSnapshot> _translationUiCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly object _translationUiCacheLock = new();
        private bool _followTranslationFile;

        public event Action? StreamingTextScrolled;
        public event Action? ConsoleOutputScrolled;

        private sealed class LocalizationSnapshot
        {
            public required List<LocalizationItem> Items { get; init; }
            public required string RawContent { get; init; }
        }

        public MainViewModel(
            LocalizationDocumentStore localizationDocumentStore,
            IZipService zipService,
            IFileService fileService,
            IRequirementsService requirementsService,
            ITranslationModeNodeLoader modeNodeLoader,
            ITranslationArchiveService archiveService,
            IDialogService dialogService,
            IGpt4FreeService gpt4FreeService)
        {
            _localizationDocumentStore = localizationDocumentStore ?? throw new ArgumentNullException(nameof(localizationDocumentStore));
            _zipService = zipService ?? throw new ArgumentNullException(nameof(zipService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _gpt4FreePrerequisitesService = requirementsService ?? throw new ArgumentNullException(nameof(requirementsService));
            _modeNodeLoader = modeNodeLoader ?? throw new ArgumentNullException(nameof(modeNodeLoader));
            _archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _gpt4FreeService = gpt4FreeService ?? throw new ArgumentNullException(nameof(gpt4FreeService));

            InitializeCollections();
            InitializeCommands();
            InitializeModes();

            UpdateStreamingTextLogoVisibility();
            UpdateConsoleOutputLogoVisibility();

            _gpt4FreeService.LogFeed.PropertyChanged += OnConsoleOutputChanged;
        }

        #region Properties

        public string TranslationButtonText => IsTranslating ? Properties.Resources.TranslationCancel : Properties.Resources.RunTranslation;
        public ObservableCollection<LocalizationItem> LocalizationStrings => _localizationDocumentStore.LocalizationStrings;
        public ObservableCollection<TreeNodeItem> TreeNodes { get; } = [];
        public ObservableCollection<TranslationModeItem> Modes { get; } = [];
        public ICollectionView? DataGridCollectionView { get; private set; }
        public ICollectionView? TreeNodesCollectionView { get; private set; }


        private TranslationModeItem? _selectedMode;
        public TranslationModeItem? SelectedMode
        {
            get => _selectedMode;
            set => SetProperty(ref _selectedMode, value, () => _ = OnComboBoxItemSelectedAsync());
        }

        private TreeNodeItem? _selectedTreeNode;
        public TreeNodeItem? SelectedTreeNode
        {
            get => _selectedTreeNode;
            set => SetProperty(ref _selectedTreeNode, value);
        }

        /// <summary>
        /// Sets the mode without invoking the callback (for programmatic changes).
        /// </summary>
        public void SetSelectedModeSilently(TranslationModeItem? mode)
        {
            if (_selectedMode != mode)
            {
                _selectedMode = mode;
                OnPropertyChanged(nameof(SelectedMode));
            }
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

        private string _localizationText = string.Empty;
        public string LocalizationText
        {
            get => _localizationText;
            set => SetProperty(ref _localizationText, value);
        }

        private string _streamingText = string.Empty;
        public string StreamingText
        {
            get => _streamingText;
            set
            {
                if (SetProperty(ref _streamingText, value))
                {
                    UpdateStreamingTextLogoVisibility();
                    StreamingTextScrolled?.Invoke();
                }
            }
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


        private bool _isStreamingTextLogoVisible = true;
        public bool IsStreamingTextLogoVisible
        {
            get => _isStreamingTextLogoVisible;
            private set
            {
                if (_isStreamingTextLogoVisible != value)
                {
                    _isStreamingTextLogoVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isConsoleOutputLogoVisible = true;
        public bool IsConsoleOutputLogoVisible
        {
            get => _isConsoleOutputLogoVisible;
            private set
            {
                if (_isConsoleOutputLogoVisible != value)
                {
                    _isConsoleOutputLogoVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Visibility of console content (text boxes, scroll bars, splitter) - hidden when collapsed.
        /// </summary>
        public bool IsConsoleContentVisible => !IsStreamingButtonCollapsed;

        /// <summary>
        /// Minimum console row height (in pixels) - depends on collapsed state.
        /// </summary>
        public double ConsoleRowMinHeight => IsStreamingButtonCollapsed ? 40.0 : 220.0;


        private bool _isTranslating;
        public bool IsTranslating
        {
            get => _isTranslating;
            set => SetProperty(ref _isTranslating, value, () =>
            {
                OnPropertyChanged(nameof(TranslationButtonText));
                OnPropertyChanged(nameof(IsTranslationInProgress));
                OnPropertyChanged(nameof(IsRowClickSelectionActive));
                OnPropertyChanged(nameof(IsRowDragSelectionActive));
            });
        }

        private bool _isRawViewMode = false;
        public bool IsRawViewMode
        {
            get => _isRawViewMode;
            set => SetProperty(ref _isRawViewMode, value, RefreshDataOnViewModeChanged);
        }

        private bool _isStreamingButtonCollapsed = true;
        public bool IsStreamingButtonCollapsed
        {
            get => _isStreamingButtonCollapsed;
            set
            {
                if (SetProperty(ref _isStreamingButtonCollapsed, value))
                {
                    CollapseConsole();
                    UpdateStreamingTextLogoVisibility();
                    UpdateConsoleOutputLogoVisibility();
                    OnPropertyChanged(nameof(IsConsoleContentVisible));
                    OnPropertyChanged(nameof(ConsoleRowMinHeight));
                }
            }
        }

        private bool _showConsoleOutput = false;
        public bool ShowConsoleOutput
        {
            get => _showConsoleOutput;
            set => SetProperty(ref _showConsoleOutput, value, UpdateConsoleOutputLogoVisibility);
        }

        private bool _isRowClickSelectionEnabled;
        public bool IsRowClickSelectionEnabled
        {
            get => _isRowClickSelectionEnabled;
            set
            {
                if (SetProperty(ref _isRowClickSelectionEnabled, value) && value)
                {
                    if (IsRowDragSelectionEnabled)
                        IsRowDragSelectionEnabled = false;
                }

                OnPropertyChanged(nameof(IsRowClickSelectionActive));
            }
        }

        private bool _isRowDragSelectionEnabled;
        public bool IsRowDragSelectionEnabled
        {
            get => _isRowDragSelectionEnabled;
            set
            {
                if (SetProperty(ref _isRowDragSelectionEnabled, value) && value)
                {
                    if (IsRowClickSelectionEnabled)
                        IsRowClickSelectionEnabled = false;
                }

                OnPropertyChanged(nameof(IsRowDragSelectionActive));
            }
        }

        public bool IsRowClickSelectionActive => IsRowClickSelectionEnabled && !IsTranslating;
        public bool IsRowDragSelectionActive => IsRowDragSelectionEnabled && !IsTranslating;


        private string _consoleOutputText = string.Empty;
        public string ConsoleOutputText
        {
            get => _consoleOutputText;
            set => SetProperty(ref _consoleOutputText, value, UpdateConsoleOutputLogoVisibility);
        }


        private GridLength _streamingTextRowHeight = new GridLength(40, GridUnitType.Pixel);
        private bool _isUpdatingConsoleState = false;
        private bool _isAutoAdjustingHeight = false;
        public GridLength StreamingTextRowHeight
        {
            get => _streamingTextRowHeight;
            set
            {
                if (_streamingTextRowHeight != value)
                {
                    if (_isAutoAdjustingHeight && !_isUpdatingConsoleState)
                    {
                        return;
                    }
                    
                    _streamingTextRowHeight = value;
                    OnPropertyChanged(nameof(StreamingTextRowHeight));
                    
                    UpdateStreamingTextLogoVisibility();
                    UpdateConsoleOutputLogoVisibility();
                }
            }
        }

        private bool _useGpt4Free = false;
        public bool UseGpt4Free
        {
            get => _useGpt4Free;
            set
            {
                if (value && !_useGpt4Free)
                {
                    bool continueWithGpt4Free = _dialogService.ShowConfirmation(Resources.Gpt4FreeWarningMessage, Resources.DialogServiceInformationTitle);

                    if (!continueWithGpt4Free)
                    {
                        OnPropertyChanged(nameof(UseGpt4Free));
                        return;
                    }
                }

                SetProperty(ref _useGpt4Free, value);
            }
        }


        public bool IsTranslationInProgress => IsTranslating;

        private GridLength _treeViewColumn = new GridLength(200, GridUnitType.Pixel);
        public GridLength TreeViewColumn
        {
            get => _treeViewColumn;
            set
            {
                _treeViewColumn = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Minimum height for displaying console logos (in pixels).
        /// </summary>
        public const double ConsoleLogoVisibilityThreshold = 170.0;



        #endregion

        #region Commands

        public ICommand? SaveTranslationCommand { get; private set; }
        public ICommand? RunTranslationCommand { get; private set; }
        public ICommand? OpenSettingsCommand { get; private set; }
        public ICommand? OpenDirectoryCommand { get; private set; }
        public ICommand? OpenFileCommand { get; private set; }
        public ICommand? OpenResourcePackCommand { get; private set; }
        public ICommand? OnTreeViewItemSelectedCommand { get; private set; }
        public ICommand? SelectMissingTargetLocalesCommand { get; private set; }
        public ICommand? OpenSelectedNodeInExplorerCommand { get; private set; }
        public ICommand? CopySelectedCellCommand { get; private set; }
        public ICommand? OnApplicationExitCommand { get; private set; }
        public ICommand? ToggleViewModeCommand { get; private set; }
        public ICommand? CollapseConsoleCommand { get; private set; }
        public ICommand? ToggleConsoleOutputCommand { get; private set; }

        #endregion

    }
}










