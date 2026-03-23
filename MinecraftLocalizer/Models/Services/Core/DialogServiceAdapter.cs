using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.ViewModels;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Core
{
    public sealed class DialogServiceAdapter : IDialogService
    {
        public void ShowError(string message) => DialogService.ShowError(message);

        public void ShowSuccess(string message, string? archivePath = null) =>
            DialogService.ShowSuccess(message, archivePath);

        public void ShowInformation(string message) => DialogService.ShowInformation(message);

        public bool ShowConfirmation(string message, string title) =>
            DialogService.ShowConfirmation(message, title);

        public void ShowDialog<T>(Window owner, ViewModelBase viewModel) where T : Window, new() =>
            DialogService.ShowDialog<T>(owner, viewModel);

        public bool? ShowRequirementsDialog(RequirementsViewModel viewModel) =>
            DialogService.ShowRequirementsDialog(viewModel);

        public void ShowConsoleOutputDialog(LogViewModel viewModel) =>
            DialogService.ShowConsoleOutputDialog(viewModel);
    }
}



