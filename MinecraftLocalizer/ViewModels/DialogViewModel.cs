using MinecraftLocalizer.Commands;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.IO;
using MinecraftLocalizer.Models.Services.Core;

namespace MinecraftLocalizer.ViewModels
{
    public class DialogViewModel : ViewModelBase
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private string? _message;
        public string? Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        private DialogType _dialogType;
        public DialogType DialogType
        {
            get => _dialogType;
            set => SetProperty(ref _dialogType, value);
        }

        private bool _isConfirmation;
        public bool IsConfirmation
        {
            get => _isConfirmation;
            set => SetProperty(ref _isConfirmation, value);
        }

        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        private string? _archivePath;
        public string? ArchivePath
        {
            get => _archivePath;
            set
            {
                if (SetProperty(ref _archivePath, value))
                {
                    (OpenArchiveCommand as RelayCommand)
                        ?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand OkCommand { get; }
        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand OpenArchiveCommand { get; }

        public DialogViewModel()
        {
            OkCommand = new RelayCommand(() => SetResult(true));
            YesCommand = new RelayCommand(() => SetResult(true));
            NoCommand = new RelayCommand(() => SetResult(false)); 
            CloseCommand = new RelayCommand<Window>(CloseWindow);
            OpenArchiveCommand = new RelayCommand(OpenArchive);
        }

        private void SetResult(bool result)
        {
            DialogResult = result;
            if (DialogResult.HasValue)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this)?
                        .Close());
            }
        }

        private void OpenArchive()
        {
            if (string.IsNullOrWhiteSpace(ArchivePath))
            {
                return;
            }

            var path = Path.GetFullPath(ArchivePath);

            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
                return;
            }

            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
            }
        }

        private void CloseWindow(Window? window)
        {
            window?.Close();
        }
    }
}

