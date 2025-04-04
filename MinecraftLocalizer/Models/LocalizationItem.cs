using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MinecraftLocalizer.Models
{
    public class LocalizationItem : INotifyPropertyChanged
    {
        public Type? DataType { get; set; }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private int _rowNumber;
        public int RowNumber
        {
            get => _rowNumber;
            set => SetProperty(ref _rowNumber, value);
        }

        private string? _id;
        public string? ID
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string? _originalString;
        public string? OriginalString
        {
            get => _originalString;
            set => SetProperty(ref _originalString, value);
        }

        private string? _translatedString;
        public string? TranslatedString
        {
            get => _translatedString;
            set => SetProperty(ref _translatedString, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}