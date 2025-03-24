using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    public class NodesToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IEnumerable children && children.Cast<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
