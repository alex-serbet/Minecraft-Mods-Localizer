using MinecraftLocalizer.ViewModels;
using MinecraftLocalizer.Views;
using System.Media;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Core
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

        public static void ShowSuccess(string message, string? archivePath = null)
            => ShowDialog(ResourceManager.DialogServiceSuccessTitle, message, DialogType.Success, SystemSounds.Asterisk, false, archivePath);

        public static void ShowInformation(string message)
            => ShowDialog(ResourceManager.DialogServiceInformationTitle, message, DialogType.Information, SystemSounds.Asterisk);

        public static bool ShowConfirmation(string message, string title)
        {
            return ShowDialog(title, message, DialogType.Confirmation, SystemSounds.Asterisk, true);
        }

        public static void ShowDialog<T>(Window owner, ViewModelBase viewModel) where T : Window, new()
        {
            var dialog = new T
            {
                Owner = owner,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            dialog.ShowDialog();
        }

        public static bool? ShowRequirementsDialog(RequirementsViewModel viewModel)
        {
            var dialog = new RequirementsView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            return dialog.ShowDialog();
        }

        public static void ShowConsoleOutputDialog(LogViewModel viewModel)
        {
            var dialog = new LogView
            {
                Owner = Application.Current.MainWindow,
                DataContext = viewModel,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            dialog.Show();
        }

        private static bool ShowDialog(
           string title,
           string message,
           DialogType type,
           SystemSound sound,
           bool isConfirmation = false,
           string? archivePath = null)
        {
            sound?.Play();

            var viewModel = new DialogViewModel
            {
                Title = title,
                Message = message,
                DialogType = type,
                IsConfirmation = isConfirmation,
                ArchivePath = archivePath
            };

            var window = new DialogView
            {
                DataContext = viewModel
            };

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


