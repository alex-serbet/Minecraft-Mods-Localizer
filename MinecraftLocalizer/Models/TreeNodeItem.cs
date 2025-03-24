using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MinecraftLocalizer.Models
{
    public class TreeNodeItem : INotifyPropertyChanged
    {
        public ObservableCollection<TreeNodeItem> ChildrenNodes { get; set; } = [];
        public bool IsRoot { get; set; }
        public required string FileName { get; set; }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
