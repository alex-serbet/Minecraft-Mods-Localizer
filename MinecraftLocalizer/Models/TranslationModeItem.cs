using System.ComponentModel;

namespace MinecraftLocalizer.Models
{
    public enum TranslationModeType
    {
        NotSelected,
        Quests,
        Mods
    }

    public class TranslationModeItem: INotifyPropertyChanged
    {
        private TranslationModeType _type;
        private string? _modeTitle;

        public TranslationModeType Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public string? ModeTitle
        {
            get => _modeTitle;
            set
            {
                if (_modeTitle == value) return;
                _modeTitle = value;
                OnPropertyChanged(nameof(ModeTitle));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
