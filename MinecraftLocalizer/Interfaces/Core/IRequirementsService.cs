using MinecraftLocalizer.Models.Services.Core;

namespace MinecraftLocalizer.Interfaces.Core
{
    /// <summary>
    /// Service for checking and installing GPT4Free dependencies
    /// </summary>
    public interface IRequirementsService
    {
        /// <summary>
        /// Checks all required dependencies
        /// </summary>
        Task<bool> EnsureRequirementsAsync();

        /// <summary>
        /// Checks if Python 3.10+ is installed
        /// </summary>
        Task<bool> CheckPythonAsync();

        /// <summary>
        /// Checks if Git is installed
        /// </summary>
        Task<bool> CheckGitAsync();

        /// <summary>
        /// Downloads and installs Python
        /// </summary>
        Task<bool> InstallPythonAsync(
            IProgress<DownloadProgress> progress);

        /// <summary>
        /// Downloads and installs Git
        /// </summary>
        Task<bool> InstallGitAsync(
            IProgress<DownloadProgress> progress);

        /// <summary>
        /// Checks for portable Python version
        /// </summary>
        bool CheckPortablePython();

        /// <summary>
        /// Opens the Python download page
        /// </summary>
        void OpenPythonPage();

        /// <summary>
        /// Opens the Git download page
        /// </summary>
        void OpenGitPage();
    }
}





