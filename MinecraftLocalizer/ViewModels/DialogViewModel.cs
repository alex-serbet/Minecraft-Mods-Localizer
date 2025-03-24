using MinecraftLocalizer.Commands;
using MinecraftLocalizer.Models.Services;
using System.Windows;
using System.Windows.Input;

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

        public ICommand OkCommand { get; }
        public ICommand YesCommand { get; }
        public ICommand NoCommand { get; }
        public ICommand CloseCommand { get; }

       
        public DialogViewModel()
        {
            OkCommand = new RelayCommand(() => SetResult(true));
            YesCommand = new RelayCommand(() => SetResult(true));
            NoCommand = new RelayCommand(() => SetResult(false)); 
            CloseCommand = new RelayCommand<Window>(CloseWindow);
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

        private void CloseWindow(Window? window)
        {
            window?.Close();
        }
    }
}