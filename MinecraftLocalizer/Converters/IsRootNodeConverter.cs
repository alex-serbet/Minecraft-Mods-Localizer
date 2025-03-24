using System.Globalization;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    public class IsRootNodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool hasItems && hasItems;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
