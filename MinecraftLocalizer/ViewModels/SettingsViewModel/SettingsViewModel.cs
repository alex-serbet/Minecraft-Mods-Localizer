using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Interfaces.Core;
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

            SaveSettingsCommand = new RelayCommand<object>(SaveSettings);
            OpenAboutWindowCommand = new RelayCommand(OpenAboutWindow);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);
            CloseWindowSettingsCommand = new RelayCommand<object>(CloseWindowSettings);
            SelectDirectoryPathCommand = new RelayCommand<object>(SelectDirectoryPath);
            ResetPromptCommand = new RelayCommand(ResetPrompt);

            _hasProviderStatusItem = false;
            _ = LoadProvidersAsync();
        }
    }
}


