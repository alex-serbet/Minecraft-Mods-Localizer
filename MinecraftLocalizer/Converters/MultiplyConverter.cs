using System.Globalization;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0d;

            if (!TryToDouble(value, out double baseValue))
                return 0d;

            if (!TryToDouble(parameter, out double factor))
                return baseValue;

            return baseValue * factor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;

        private static bool TryToDouble(object input, out double result)
        {
            switch (input)
            {
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case string s:
                    return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                default:
                    try
                    {
                        result = System.Convert.ToDouble(input, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = 0d;
                        return false;
                    }
            }
        }
    }
}
