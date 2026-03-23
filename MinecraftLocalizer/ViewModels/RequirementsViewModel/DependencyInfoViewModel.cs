using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models.Services.Core;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels
{
    public sealed class DependencyInfoViewModel : ViewModelBase
    {
        private readonly RequirementsViewModel _parent;
        private readonly Func<IProgress<DownloadProgress>, Task<bool>> _installFunc;

        private bool _installed;
        private bool _isDownloading;
        private DownloadProgress _progress;

        public string Name { get; }
        public Func<Task<bool>> CheckFunc { get; }
        public Action DownloadLinkAction { get; }

        public ICommand CheckCommand { get; }
        public ICommand OpenDownloadLinkCommand { get; }
        public ICommand InstallCommand { get; }

        public string Status
        {
            get
            {
                if (IsDownloading)
                {
                    if (!string.IsNullOrEmpty(Progress.Status) && Progress.Status.Contains(':'))
                        return $"? {Progress.Status}";

                    return $"? {Properties.Resources.Downloading} {Progress.Progress:0}%";
                }

                return Installed
                    ? $"? {Properties.Resources.Installed}"
                    : $"? {Properties.Resources.NotInstalled}";
            }
        }

        public bool Installed
        {
            get => _installed;
            set
            {
                if (SetProperty(ref _installed, value))
                    OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                    OnPropertyChanged(nameof(Status));
            }
        }

        public DownloadProgress Progress
        {
            get => _progress;
            set
            {
                _progress.PropertyChanged -= Progress_PropertyChanged;
                _progress = value;
                _progress.PropertyChanged += Progress_PropertyChanged;

                OnPropertyChanged();
                OnPropertyChanged(nameof(Status));
            }
        }

        public DependencyInfoViewModel(
            RequirementsViewModel parent,
            string name,
            Func<Task<bool>> checkFunc,
            Action downloadLink,
            Func<IProgress<DownloadProgress>, Task<bool>> installFunc)
        {
            _parent = parent;
            Name = name;
            CheckFunc = checkFunc;
            DownloadLinkAction = downloadLink;
            _installFunc = installFunc;
            _progress = new DownloadProgress(name);
            _progress.PropertyChanged += Progress_PropertyChanged;

            CheckCommand = new RelayCommand(async () => await CheckAsync());
            OpenDownloadLinkCommand = new RelayCommand(DownloadLinkAction);
            InstallCommand = new RelayCommand(async () => await _parent.DownloadInstallAsync(this, _installFunc));
        }

        public async Task<bool> CheckAsync()
        {
            Installed = await CheckFunc();
            return Installed;
        }

        private void Progress_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DownloadProgress.Progress) || e.PropertyName == nameof(DownloadProgress.Status))
                OnPropertyChanged(nameof(Status));
        }
    }
}
