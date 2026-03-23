using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MinecraftLocalizer.Models.Services.Core
{
    public class DownloadProgress : INotifyPropertyChanged
    {
        private string _componentName = string.Empty;
        private double _progress;
        private string _status = string.Empty;
        private bool _isDownloading;
        private bool _isCompleted;
        private bool _hasError;

        public string ComponentName
        {
            get => _componentName;
            set => SetProperty(ref _componentName, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set => SetProperty(ref _isDownloading, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set => SetProperty(ref _isCompleted, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public DownloadProgress(string componentName)
        {
            ComponentName = componentName;
            Progress = 0;
            Status = "Waiting";
            IsDownloading = false;
            IsCompleted = false;
            HasError = false;
        }

        public void Reset()
        {
            Progress = 0;
            Status = "Waiting";
            IsDownloading = false;
            IsCompleted = false;
            HasError = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}



