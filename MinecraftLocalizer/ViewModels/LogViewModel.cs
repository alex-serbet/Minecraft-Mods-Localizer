using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Interfaces.Ai;
using MinecraftLocalizer.Interfaces.Core;
using System.Windows.Input;

namespace MinecraftLocalizer.ViewModels
{
    public class LogViewModel : ViewModelBase
    {
        private readonly IGpt4FreeService _gpt4FreeService;
        private string _output = string.Empty;
        private double _progress;
        private string _status = Properties.Resources.Preparing;
        private bool _isRunning;
        public ICommand CancelCommand { get; }
        public LogViewModel(IGpt4FreeService gpt4FreeService)
        {
            _gpt4FreeService = gpt4FreeService;

            _gpt4FreeService.LogFeed.PropertyChanged += OnConsoleOutputChanged;

            UpdateFromConsoleOutput();

            CloseCommand = new RelayCommand(Close);
            CancelCommand = new RelayCommand(async () => await CancelInstallationAsync(), () => IsRunning);
        }
        private async Task CancelInstallationAsync()
        {
            if (_gpt4FreeService != null && _gpt4FreeService.LogFeed.IsRunning)
            {
                await _gpt4FreeService.CancelInstallationAsync();
            }
        }

        /// <summary>
        /// Full console output.
        /// </summary>
        public string Output
        {
            get => _output;
            set => SetProperty(ref _output, value);
        }

        /// <summary>
        /// Current progress.
        /// </summary>
        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        /// <summary>
        /// Current status.
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Process running flag.
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        /// <summary>
        /// Close window command.
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Handler for console output changes.
        /// </summary>
        private void OnConsoleOutputChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateFromConsoleOutput();

            if (e.PropertyName == nameof(ILogFeed.IsRunning) && !IsRunning)
            {
                CloseWindow();
            }
        }

        /// <summary>
        /// Updates properties from LogFeed.
        /// </summary>
        private void UpdateFromConsoleOutput()
        {
            try
            {
                // Check that the service has not been disposed.
                if (_gpt4FreeService != null)
                {
                    Output = _gpt4FreeService.LogFeed.Output;
                    Progress = _gpt4FreeService.LogFeed.Progress;
                    Status = _gpt4FreeService.LogFeed.Status;
                    IsRunning = _gpt4FreeService.LogFeed.IsRunning;
                }
            }
            catch (ObjectDisposedException)
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void Close()
        {
            // Unsubscribe from events.
            _gpt4FreeService.LogFeed.PropertyChanged -= OnConsoleOutputChanged;

            // Close the window.
            CloseWindow();
        }

        /// <summary>
        /// Closes the window.
        /// </summary>
        private void CloseWindow()
        {
            foreach (var window in System.Windows.Application.Current.Windows)
            {
                if (window is System.Windows.Window w && w.DataContext == this)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}

