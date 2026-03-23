using System;
using System.Globalization;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    /// <summary>
    /// Converter for calculating the maximum height of the console row.
    /// Subtracts the main row height (300px) and the GridSplitter height (5px) from the total height.
    /// </summary>
    public class MaxHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualHeight)
            {
                // Subtract the minimum main row height (300px) and the GridSplitter height (5px).
                // ActualHeight is the Grid height, which already excludes the top rows (45px + 40px).
                double reservedHeight = 300 + 5; // 305px

                // Keep at least 40px for the console (its MinHeight).
                double maxHeight = actualHeight - reservedHeight;
                return maxHeight < 40 ? 40 : maxHeight;
            }
            
            return double.PositiveInfinity;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
