using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    public class CheckBoxNodeVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return Visibility.Visible;

            string modeType = values[0] switch
            {
                Enum enumValue => enumValue.ToString(),
                string str => str,
                _ => values[0].ToString() ?? string.Empty
            };

            bool isRoot = values[1] switch
            {
                bool b => b,
                string s when bool.TryParse(s, out bool result) => result,
                _ => false
            };

            if (string.Equals(modeType, "Patchouli", StringComparison.OrdinalIgnoreCase))
            {
                return !isRoot ? Visibility.Collapsed : Visibility.Visible;
            }

            return isRoot ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}