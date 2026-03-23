using System.ComponentModel;

namespace MinecraftLocalizer.Interfaces.Core
{
    /// <summary>
    /// Interface for the console output model.
    /// </summary>
    public interface ILogFeed : INotifyPropertyChanged
    {
        /// <summary>
        /// Full console output.
        /// </summary>
        string Output { get; set; }

        /// <summary>
        /// Execution progress (0-100).
        /// </summary>
        double Progress { get; set; }

        /// <summary>
        /// Current status.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// Process running flag.
        /// </summary>
        bool IsRunning { get; set; }

        /// <summary>
        /// Appends a new line to output.
        /// </summary>
        void AppendLine(string line);

        /// <summary>
        /// Resets state.
        /// </summary>
        void Reset();
    }
}



