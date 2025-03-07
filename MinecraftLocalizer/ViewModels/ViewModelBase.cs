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

        // Базовая версия метода
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            return SetProperty(ref field, value, null, propertyName);
        }

        // Версия с callback'ом
        protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke();
            return true;
        }

        // Версия с callback'ом и параметром
        protected bool SetProperty<T>(ref T field, T value, Action<T> onChanged, T parameter, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            onChanged?.Invoke(parameter);
            return true;
        }
    }
}