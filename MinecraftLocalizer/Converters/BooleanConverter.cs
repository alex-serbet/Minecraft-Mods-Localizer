using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    /// <summary>
    /// Universal converter for working with boolean values.
    /// Supports conversion to Visibility, inversion, and additional options.
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            var options = ParseOptions(parameter);

            if (options.Invert)
                boolValue = !boolValue;

            // If the target type is Visibility, return Visibility
            if (targetType == typeof(Visibility))
            {
                if (boolValue)
                    return Visibility.Visible;

                return options.Hidden ? Visibility.Hidden : Visibility.Collapsed;
            }

            // If the target type is bool, return bool (e.g., for inversion)
            if (targetType == typeof(bool))
            {
                return boolValue;
            }

            // By default, return bool
            return boolValue;
        }

        private static (bool Invert, bool Hidden) ParseOptions(object parameter)
        {
            if (parameter is not string param)
                return (false, false);

            var opts = param.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool invert = opts.Any(o => o.Equals("Invert", StringComparison.OrdinalIgnoreCase));
            bool hidden = opts.Any(o => o.Equals("Hidden", StringComparison.OrdinalIgnoreCase));
            return (invert, hidden);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // For reverse conversion from Visibility to bool
            if (value is Visibility visibility)
            {
                var options = ParseOptions(parameter);
                bool result = visibility == Visibility.Visible;

                if (options.Invert)
                    result = !result;

                return result;
            }

            // For reverse conversion from bool to bool (inversion)
            if (value is bool boolValue)
            {
                var options = ParseOptions(parameter);

                if (options.Invert)
                    return !boolValue;

                return boolValue;
            }

            return false;
        }
    }
}