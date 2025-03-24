using MinecraftLocalizer.ViewModels;
using MinecraftLocalizer.Views;
using System.Media;
using System.Windows;

namespace MinecraftLocalizer.Models.Services
{
    public enum DialogType
    {
        Information,
        Success,
        Error,
        Confirmation
    }
    public static class DialogService
    {
        public static void ShowError(string message)
            => ShowDialog(ResourceManager.DialogServiceErrorTitle, message, DialogType.Error, SystemSounds.Hand);

        public static void ShowSuccess(string message)
            => ShowDialog(ResourceManager.DialogServiceSuccessTitle, message, DialogType.Success, SystemSounds.Asterisk);

        public static void ShowInformation(string message)
            => ShowDialog(ResourceManager.DialogServiceInformationTitle, message, DialogType.Information, SystemSounds.Asterisk);

        public static bool ShowConfirmation(string message, string title)
        {
            return ShowDialog(title, message, DialogType.Confirmation, SystemSounds.Asterisk, true);
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

        private static bool ShowDialog(
           string title,
           string message,
           DialogType type,
           SystemSound sound,
           bool isConfirmation = false)
        {
            sound?.Play();

            var window = new DialogView();
            var viewModel = (DialogViewModel)window.DataContext;

            viewModel.Title = title;
            viewModel.Message = message;
            viewModel.DialogType = type;
            viewModel.IsConfirmation = isConfirmation;

            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.ShowDialog();

            return viewModel.DialogResult ?? false;
        }
    }

    internal class ResourceManager
    {
        public static string DialogServiceErrorTitle => Properties.Resources.DialogServiceErrorTitle;
        public static string DialogServiceSuccessTitle => Properties.Resources.DialogServiceSuccessTitle;
        public static string DialogServiceInformationTitle => Properties.Resources.DialogServiceInformationTitle;
    }
}