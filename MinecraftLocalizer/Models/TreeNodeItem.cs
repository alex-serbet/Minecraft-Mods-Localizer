using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MinecraftLocalizer.Models
{
    public class TreeNodeItem : INotifyPropertyChanged
    {
        public ObservableCollection<TreeNodeItem> ChildrenNodes { get; set; } = [];
        public bool IsRoot { get; set; }
        public bool HasItems => ChildrenNodes.Any();
        public required string FileName { get; set; }
        public required string FilePath { get; set; }
        public required string ModPath { get; set; }


        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        private bool _isTranslating;
        public bool IsTranslating
        {
            get => _isTranslating;
            set
            {
                if (_isTranslating != value)
                {
                    _isTranslating = value;
                    OnPropertyChanged(nameof(IsTranslating));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
