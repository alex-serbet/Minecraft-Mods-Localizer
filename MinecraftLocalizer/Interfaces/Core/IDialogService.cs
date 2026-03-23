using MinecraftLocalizer.ViewModels;

namespace MinecraftLocalizer.Interfaces.Core
{
    /// <summary>
    /// Interface for dialog window service.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows an error dialog.
        /// </summary>
        void ShowError(string message);

        /// <summary>
        /// Shows a success dialog.
        /// </summary>
        void ShowSuccess(string message, string? archivePath = null);

        /// <summary>
        /// Shows an information dialog.
        /// </summary>
        void ShowInformation(string message);

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        bool ShowConfirmation(string message, string title);

        /// <summary>
        /// Shows a dialog with a custom window type.
        /// </summary>
        void ShowDialog<T>(System.Windows.Window owner, ViewModelBase viewModel) where T : System.Windows.Window, new();

        /// <summary>
        /// Shows a dependencies check dialog.
        /// </summary>
        bool? ShowRequirementsDialog(RequirementsViewModel viewModel);

        /// <summary>
        /// Shows a console output dialog.
        /// </summary>
        void ShowConsoleOutputDialog(LogViewModel viewModel);
    }
}



