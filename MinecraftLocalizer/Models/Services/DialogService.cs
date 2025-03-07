using System.Windows;

namespace MinecraftLocalizer.Models.Services
{

    public class DialogService
    {
        public static void ShowError(string message)
        {
            MessageBox.Show(message, Properties.Resources.DialogServiceError, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowSuccess(string message)
        {
            MessageBox.Show(message, Properties.Resources.DialogServiceSuccess, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static void ShowInformation(string message)
        {
            MessageBox.Show(message, Properties.Resources.DialogServiceInformation, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        public static bool ShowConfirmation(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        public static void ShowDialog<T>(Window owner) where T : Window, new()
        {
            var dialog = new T
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            dialog.ShowDialog();
        }

        public static void CloseDialog(Window window)
        {
            window.Close();
        }
    }
}
