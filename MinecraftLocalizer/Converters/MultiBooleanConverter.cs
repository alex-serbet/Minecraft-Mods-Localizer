using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MinecraftLocalizer.Converters
{
    public class MultiBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            var options = ParseOptions(parameter);

            // Logic for StreamingTextBox: show when streaming is expanded AND console output is NOT shown
            if (options.IsStreamingTextBox)
            {
                bool isStreamingButtonCollapsed = values[0] is bool b1 && b1;
                bool showConsoleOutput = values[1] is bool b2 && b2;
                return isStreamingButtonCollapsed && !showConsoleOutput
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            // Logic for ConsoleTextBox: show when streaming is expanded AND console output is shown
            else if (options.IsConsoleTextBox)
            {
                bool isStreamingButtonCollapsed = values[0] is bool b1 && b1;
                bool showConsoleOutput = values[1] is bool b2 && b2;
                return isStreamingButtonCollapsed && showConsoleOutput
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            // Logic for StreamingLogo: show when logo is visible, streaming is expanded AND console output is NOT shown
            else if (options.IsStreamingLogo)
            {
                bool isLogoVisible = values[0] is bool b1 && b1;
                bool showConsoleOutput = values[1] is bool b2 && b2;
                bool isStreamingButtonCollapsed = values.Length > 2 && values[2] is bool b3 && b3;
                return isLogoVisible && !showConsoleOutput && isStreamingButtonCollapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            // Logic for ConsoleLogo: show when logo is visible, streaming is expanded AND console output is shown
            else if (options.IsConsoleLogo)
            {
                bool isLogoVisible = values[0] is bool b1 && b1;
                bool showConsoleOutput = values[1] is bool b2 && b2;
                bool isStreamingButtonCollapsed = values.Length > 2 && values[2] is bool b3 && b3;
                return isLogoVisible && showConsoleOutput && isStreamingButtonCollapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        private static (bool IsStreamingTextBox, bool IsConsoleTextBox, bool IsStreamingLogo, bool IsConsoleLogo) ParseOptions(object parameter)
        {
            if (parameter is not string param)
                return (false, false, false, false);

            var opts = param.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool isStreamingTextBox = opts.Any(o => o.Equals("StreamingTextBox", StringComparison.OrdinalIgnoreCase));
            bool isConsoleTextBox = opts.Any(o => o.Equals("ConsoleTextBox", StringComparison.OrdinalIgnoreCase));
            bool isStreamingLogo = opts.Any(o => o.Equals("StreamingLogo", StringComparison.OrdinalIgnoreCase));
            bool isConsoleLogo = opts.Any(o => o.Equals("ConsoleLogo", StringComparison.OrdinalIgnoreCase));
            return (isStreamingTextBox, isConsoleTextBox, isStreamingLogo, isConsoleLogo);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}