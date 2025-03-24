using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MinecraftLocalizer.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Basic version of the method
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            return SetProperty(ref field, value, null, propertyName);
        }

        // Version with callback
        protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke();
            return true;
        }
    }
}