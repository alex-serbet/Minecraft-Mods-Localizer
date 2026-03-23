using MinecraftLocalizer.Interfaces.Core;

namespace MinecraftLocalizer.Interfaces.Ai
{
    /// <summary>
    /// Interface for the GPT4Free service.
    /// </summary>
    public interface IGpt4FreeService : IDisposable
    {
        /// <summary>
        /// Service console output.
        /// </summary>
        ILogFeed LogFeed { get; }

        /// <summary>
        /// Performs GPT4Free installation (only once).
        /// </summary>
        /// <returns>True if installation succeeded or was already completed.</returns>
        Task<bool> PerformInstallationAsync();

        /// <summary>
        /// Starts the GPT4Free API server.
        /// </summary>
        Task StartGpt4FreeAsync();

        /// <summary>
        /// Cancels installation (only if it is in progress).
        /// </summary>
        Task CancelInstallationAsync();

        /// <summary>
        /// Ensures the server is running.
        /// </summary>
        void EnsureServerRunning();

        void Shutdown();
    }
}




