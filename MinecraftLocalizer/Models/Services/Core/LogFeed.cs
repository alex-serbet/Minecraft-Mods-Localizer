using MinecraftLocalizer.Interfaces.Core;
using MinecraftLocalizer.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MinecraftLocalizer.Models.Services.Core
{
    /// <summary>
    /// Model for displaying real-time console output
    /// </summary>
    public class LogFeed : ILogFeed
    {
        private const int MaxOutputLines = 1000;
        private string _output = string.Empty;
        private double _progress;
        private string _status = Resources.Preparing;
        private bool _isRunning;

        /// <summary>
        /// Full console output
        /// </summary>
        public string Output
        {
            get => _output;
            set
            {
                if (_output != value)
                {
                    _output = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Execution progress (0-100).
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current status
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Process running flag
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Appends a new line to output
        /// </summary>
        public void AppendLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => AppendLine(line));
                return;
            }

            // Update status based on line content
            UpdateStatusFromLine(line);

            // Append line to output
            Output += $"{DateTime.Now:HH:mm:ss} - {line}\n";

            // Limit output size (last MaxOutputLines lines)
            var lines = Output.Split('\n');
            if (lines.Length > MaxOutputLines)
            {
                Output = string.Join("\n", lines.Skip(lines.Length - MaxOutputLines));
            }
        }

        /// <summary>
        /// Updates progress based on line content
        /// </summary>
        private void UpdateStatusFromLine(string line)
        {
            line = line.ToLower();

            // Detect stage by keywords
            if (line.Contains("cloning into"))
            {
                Status = Resources.DownloadingGpt4Free;
                Progress = 10;
            }
            else if (line.Contains("resolving deltas"))
            {
                Status = Resources.FinalizingDownloading;
                Progress = 30;
            }
            else if (line.Contains("collecting"))
            {
                Status = Resources.CollectingRequirements;
                Progress = 40;
            }
            else if (line.Contains("installing collected packages"))
            {
                Status = Resources.InstallingPackages;
                Progress = 60;
            }
            else if (line.Contains("successfully installed"))
            {
                Status = Resources.RequirementsInstalled;
                Progress = 90;
            }
            else if (line.Contains("running"))
            {
                Status = Resources.LaunchingGpt4Free;
                Progress = 100;
            }
            else if (line.Contains("error"))
            {
                Status = Resources.ErrorDetected;
            }
        }

        /// <summary>
        /// Resets state
        /// </summary>
        public void Reset()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(Reset);
                return;
            }

            Output = string.Empty;
            Progress = 0;
            Status = Resources.Preparing;
            IsRunning = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler == null)
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                dispatcher.Invoke(() => handler(this, new PropertyChangedEventArgs(propertyName)));
            }
        }
    }
}

