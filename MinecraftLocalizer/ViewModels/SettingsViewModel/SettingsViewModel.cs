using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Models.Localization.Requests;
using MinecraftLocalizer.Models.Services.Core;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        private const string Gpt4FreeProvidersEndpoint = "http://localhost:1337/v1/providers";
        private const double MinTemperature = 0.1;
        private const double MaxTemperature = 1.5;
        private const int MinBatchSize = 1;
        private const int MaxBatchSize = 500;

        [GeneratedRegex(@"\{\d+\}")]
        private static partial Regex PromptVariableRegex();


        public event Action<bool>? SettingsClosed;
        public ObservableCollection<LanguageOption> Languages { get; set; }
        public ObservableCollection<LanguageOption> ProgramLanguages { get; set; }
        public ObservableCollection<ProviderOption> Providers { get; }
        public ObservableCollection<string> Models { get; }
        public ObservableCollection<string> GeminiModels { get; }

        public string[] RightPanelOptions { get; } = ["DeepSeek", "Gemini"];

        private string _selectedRightPanelProvider;
        public string SelectedRightPanelProvider
        {
            get => _selectedRightPanelProvider;
            set
            {
                if (SetProperty(ref _selectedRightPanelProvider, value))
                {
                    OnPropertyChanged(nameof(IsRightPanelDeepSeek));
                    OnPropertyChanged(nameof(IsRightPanelGemini));
                    if (value == "Gemini" && GeminiModels.Count == 0 && !string.IsNullOrWhiteSpace(GeminiApiKey))
                        _ = LoadGeminiModelsAsync(GeminiApiKey);
                }
            }
        }

        public bool IsRightPanelDeepSeek => _selectedRightPanelProvider == "DeepSeek";
        public bool IsRightPanelGemini => _selectedRightPanelProvider == "Gemini";


        private bool _autoSaveAfterBatch;
        public bool AutoSaveAfterBatch
        {
            get => _autoSaveAfterBatch;
            set => SetProperty(ref _autoSaveAfterBatch, value);
        }

        private string _selectedSourceLanguage;
        public string SelectedSourceLanguage
        {
            get => _selectedSourceLanguage;
            set => SetProperty(ref _selectedSourceLanguage, value);
        }

        private string _selectedTargetLanguage;
        public string SelectedTargetLanguage
        {
            get => _selectedTargetLanguage;
            set => SetProperty(ref _selectedTargetLanguage, value);
        }

        private string _selectedProgramLanguage;
        public string SelectedProgramLanguage
        {
            get => _selectedProgramLanguage;
            set => SetProperty(ref _selectedProgramLanguage, value);
        }

        private string _directoryPath;
        public string DirectoryPath
        {
            get => _directoryPath;
            set => SetProperty(ref _directoryPath, value);
        }

        private string _prompt;
        public string Prompt
        {
            get => _prompt;
            set => SetProperty(ref _prompt, value);
        }

        private string _deepSeekApiKey;
        public string DeepSeekApiKey
        {
            get => _deepSeekApiKey;
            set => SetProperty(ref _deepSeekApiKey, value);
        }

        private string _gpt4FreeApiKey;
        public string Gpt4FreeApiKey
        {
            get => _gpt4FreeApiKey;
            set => SetProperty(ref _gpt4FreeApiKey, value);
        }

        private string _selectedProviderId;
        public string SelectedProviderId
        {
            get => _selectedProviderId;
            set => SetProperty(ref _selectedProviderId, value, OnSelectedProviderChanged);
        }

        private string _selectedModelId;
        public string SelectedModelId
        {
            get => _selectedModelId;
            set => SetProperty(ref _selectedModelId, value);
        }

        private bool _hasModelStatusItem;
        private bool _hasProviderStatusItem;
        private bool _suppressProviderSelectionChangeLoad;
        private bool _isResettingToDefault;

        private string _geminiApiKey;
        public string GeminiApiKey
        {
            get => _geminiApiKey;
            set
            {
                if (SetProperty(ref _geminiApiKey, value))
                    ModContextApiKey = value;
            }
        }

        private string _selectedGeminiModelId;
        public string SelectedGeminiModelId
        {
            get => _selectedGeminiModelId;
            set => SetProperty(ref _selectedGeminiModelId, value);
        }

        private double _geminiTemperature;
        public double GeminiTemperature
        {
            get => _geminiTemperature;
            set => SetProperty(ref _geminiTemperature, Math.Round(Math.Clamp(value, MinTemperature, MaxTemperature), 1));
        }

        private int _geminiBatchSize;
        public int GeminiBatchSize
        {
            get => _geminiBatchSize;
            set => SetProperty(ref _geminiBatchSize, Math.Clamp(value, MinBatchSize, MaxBatchSize));
        }

        private bool _geminiEnableGoogleSearch;
        public bool GeminiEnableGoogleSearch
        {
            get => _geminiEnableGoogleSearch;
            set => SetProperty(ref _geminiEnableGoogleSearch, value);
        }

        private bool _geminiThinkingEnabled;
        public bool GeminiThinkingEnabled
        {
            get => _geminiThinkingEnabled;
            set => SetProperty(ref _geminiThinkingEnabled, value);
        }

        private bool _enableSearchContextEnrichment;
        public bool EnableSearchContextEnrichment
        {
            get => _enableSearchContextEnrichment;
            set => SetProperty(ref _enableSearchContextEnrichment, value);
        }

        private string _modContextApiKey = string.Empty;
        public string ModContextApiKey
        {
            get => _modContextApiKey;
            set
            {
                if (SetProperty(ref _modContextApiKey, value))
                    GeminiApiKey = value;
            }
        }

        private string _modContextSearchPrompt = string.Empty;
        public string ModContextSearchPrompt
        {
            get => _modContextSearchPrompt;
            set => SetProperty(ref _modContextSearchPrompt, value);
        }

        private bool _isModContextCollapsed;
        public bool IsModContextCollapsed
        {
            get => _isModContextCollapsed;
            set => SetProperty(ref _isModContextCollapsed, value);
        }

        private bool _isGeminiModelsLoading;
        public bool IsGeminiModelsLoading
        {
            get => _isGeminiModelsLoading;
            set => SetProperty(ref _isGeminiModelsLoading, value);
        }

        private string _geminiModelLoadError = string.Empty;
        public string GeminiModelLoadError
        {
            get => _geminiModelLoadError;
            set => SetProperty(ref _geminiModelLoadError, value);
        }



        private double _gpt4FreeTemperature;
        public double Gpt4FreeTemperature
        {
            get => _gpt4FreeTemperature;
            set => SetProperty(ref _gpt4FreeTemperature, Math.Round(Math.Clamp(value, MinTemperature, MaxTemperature), 1));
        }

        private int _gpt4FreeBatchSize;
        public int Gpt4FreeBatchSize
        {
            get => _gpt4FreeBatchSize;
            set => SetProperty(ref _gpt4FreeBatchSize, Math.Clamp(value, MinBatchSize, MaxBatchSize));
        }

        private double _deepSeekTemperature;
        public double DeepSeekTemperature
        {
            get => _deepSeekTemperature;
            set => SetProperty(ref _deepSeekTemperature, Math.Round(Math.Clamp(value, MinTemperature, MaxTemperature), 1));
        }

        private int _deepSeekBatchSize;
        public int DeepSeekBatchSize
        {
            get => _deepSeekBatchSize;
            set => SetProperty(ref _deepSeekBatchSize, Math.Clamp(value, MinBatchSize, MaxBatchSize));
        }

        private bool _isProvidersLoading;
        public bool IsProvidersLoading
        {
            get => _isProvidersLoading;
            set
            {
                if (SetProperty(ref _isProvidersLoading, value))
                {
                    OnPropertyChanged(nameof(ProviderStatusText));
                    OnPropertyChanged(nameof(IsProviderSelectionEnabled));
                }
            }
        }

        private bool _isModelsLoading;
        public bool IsModelsLoading
        {
            get => _isModelsLoading;
            set
            {
                if (SetProperty(ref _isModelsLoading, value))
                {
                    OnPropertyChanged(nameof(ModelStatusText));
                    OnPropertyChanged(nameof(IsModelSelectionEnabled));
                }
            }
        }

        private string _providerLoadError = string.Empty;
        public string ProviderLoadError
        {
            get => _providerLoadError;
            set
            {
                if (SetProperty(ref _providerLoadError, value))
                {
                    OnPropertyChanged(nameof(ProviderStatusText));
                }
            }
        }

        private string _modelLoadError = string.Empty;
        public string ModelLoadError
        {
            get => _modelLoadError;
            set
            {
                if (SetProperty(ref _modelLoadError, value))
                {
                    OnPropertyChanged(nameof(ModelStatusText));
                }
            }
        }

        public string ProviderStatusText =>
            IsProvidersLoading ? Properties.Resources.LoadingProvidersStatus : ProviderLoadError;

        public string ModelStatusText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedProviderId))
                    return string.Empty;

                if (IsModelsLoading)
                    return Properties.Resources.LoadingWindowTitle;

                if (!string.IsNullOrWhiteSpace(ModelLoadError))
                    return ModelLoadError;

                return string.Empty;
            }
        }

        public bool IsModelSelectionEnabled =>
            !IsModelsLoading &&
            !string.IsNullOrWhiteSpace(SelectedProviderId) &&
            Models.Count > 0 &&
            !_hasModelStatusItem;

        public bool IsProviderSelectionEnabled => !IsProvidersLoading && Providers.Count > 0 && !_hasProviderStatusItem;

        private int _modelRequestVersion;

        private bool _isGpt4FreeCollapsed = true;
        public bool IsGpt4FreeCollapsed
        {
            get => _isGpt4FreeCollapsed;
            set => SetProperty(ref _isGpt4FreeCollapsed, value);
        }

        private bool _isDeepSeekCollapsed = true;
        public bool IsDeepSeekCollapsed
        {
            get => _isDeepSeekCollapsed;
            set => SetProperty(ref _isDeepSeekCollapsed, value);
        }

        private bool _isGeminiCollapsed = true;
        public bool IsGeminiCollapsed
        {
            get => _isGeminiCollapsed;
            set => SetProperty(ref _isGeminiCollapsed, value);
        }

        private bool _isMainSettingsCollapsed = true;
        public bool IsMainSettingsCollapsed
        {
            get => _isMainSettingsCollapsed;
            set => SetProperty(ref _isMainSettingsCollapsed, value);
        }

        private bool _isPromptCollapsed = true;
        public bool IsPromptCollapsed
        {
            get => _isPromptCollapsed;
            set => SetProperty(ref _isPromptCollapsed, value);
        }
    
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand OpenAboutWindowCommand { get; private set; }
        public ICommand ResetToDefaultCommand { get; private set; }
        public ICommand CloseWindowSettingsCommand { get; private set; }
        public ICommand SelectDirectoryPathCommand { get; private set; }
        public ICommand ResetPromptCommand { get; private set; }
        public ICommand RefreshGeminiModelsCommand { get; private set; }

        public SettingsViewModel()
            : this(new DialogServiceAdapter())
        {
        }

        public SettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            Languages = [.. GetLanguages()];
            ProgramLanguages = [.. GetProgramLanguages()];
            Providers = [];
            Models = [];
            GeminiModels = [];

            _selectedSourceLanguage = Properties.Settings.Default.SourceLanguage;
            _selectedTargetLanguage = Properties.Settings.Default.TargetLanguage;
            _selectedProgramLanguage = Properties.Settings.Default.ProgramLanguage;
            _directoryPath = Properties.Settings.Default.DirectoryPath;
            _prompt = Properties.Settings.Default.Prompt;
            _deepSeekApiKey = Properties.Settings.Default.DeepSeekApiKey;
            _gpt4FreeApiKey = Properties.Settings.Default.Gpt4FreeApiKey;
            _selectedProviderId = Properties.Settings.Default.Gpt4FreeProviderId;
            _selectedModelId = Properties.Settings.Default.Gpt4FreeModelId;
            _gpt4FreeTemperature = Math.Round(Math.Clamp(Properties.Settings.Default.Gpt4FreeTemperature, MinTemperature, MaxTemperature), 1);
            _deepSeekTemperature = Math.Round(Math.Clamp(Properties.Settings.Default.DeepSeekTemperature, MinTemperature, MaxTemperature), 1);
            int legacyBatchSize = Math.Clamp(Properties.Settings.Default.TranslationBatchSize, MinBatchSize, MaxBatchSize);
            int configuredGptBatch = Properties.Settings.Default.Gpt4FreeBatchSize > 0
                ? Properties.Settings.Default.Gpt4FreeBatchSize
                : legacyBatchSize;
            int configuredDeepSeekBatch = Properties.Settings.Default.DeepSeekBatchSize > 0
                ? Properties.Settings.Default.DeepSeekBatchSize
                : legacyBatchSize;

            _gpt4FreeBatchSize = Math.Clamp(configuredGptBatch, MinBatchSize, MaxBatchSize);
            _deepSeekBatchSize = Math.Clamp(configuredDeepSeekBatch, MinBatchSize, MaxBatchSize);

            _selectedRightPanelProvider = "DeepSeek";
            _geminiApiKey = Properties.Settings.Default.GeminiApiKey;
            _selectedGeminiModelId = Properties.Settings.Default.GeminiModelId;
            _geminiTemperature = Math.Round(Math.Clamp(Properties.Settings.Default.GeminiTemperature, MinTemperature, MaxTemperature), 1);
            _geminiBatchSize = Math.Clamp(Properties.Settings.Default.GeminiBatchSize, MinBatchSize, MaxBatchSize);
            _geminiEnableGoogleSearch = Properties.Settings.Default.GeminiEnableGoogleSearch;
            _geminiThinkingEnabled = !Properties.Settings.Default.GeminiDisableThinking;
            _autoSaveAfterBatch = Properties.Settings.Default.AutoSaveAfterBatch;
            _enableSearchContextEnrichment = Properties.Settings.Default.EnableSearchContextEnrichment;
            _modContextApiKey = Properties.Settings.Default.ModContextApiKey ?? string.Empty;
            _modContextSearchPrompt = Properties.Settings.Default.ModContextSearchPrompt ?? string.Empty;

            SaveSettingsCommand = new RelayCommand<object>(SaveSettings);
            OpenAboutWindowCommand = new RelayCommand(OpenAboutWindow);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);
            CloseWindowSettingsCommand = new RelayCommand<object>(CloseWindowSettings);
            SelectDirectoryPathCommand = new RelayCommand<object>(SelectDirectoryPath);
            ResetPromptCommand = new RelayCommand(ResetPrompt);
            RefreshGeminiModelsCommand = new RelayCommand(() =>
            {
                if (!string.IsNullOrWhiteSpace(GeminiApiKey))
                    _ = LoadGeminiModelsAsync(GeminiApiKey);
            });

            _hasProviderStatusItem = false;
            _ = LoadProvidersAsync();


        }
    }
}


