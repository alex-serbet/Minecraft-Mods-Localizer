﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MinecraftLocalizer.Views
{
    public sealed partial class LoadingWindow : Window, IDisposable
    {
        private const int AnimationDurationMs = 300;
        private const double RandomDelayFactor = 0.5;
        private static readonly Random _random = new();

        private readonly Dictionary<ProgressStage, string> _stageDescriptions = new()
        {
            [ProgressStage.Cloning] = Properties.Resources.StageCloningGPT4Free,
            [ProgressStage.InstallingDependencies] = Properties.Resources.StageInstallingDependenciesGPT4Free,
            [ProgressStage.InstallingPackages] = Properties.Resources.StageInstallingPackagesGPT4Free
        };

        private double _currentProgress;
        private bool _disposed;
        private readonly CancellationTokenSource _animationCts;

        public LoadingWindow(Window owner)
        {
            InitializeComponent();
            InitializeWindowPosition(owner);
            _animationCts = new CancellationTokenSource();
        }

        private void InitializeWindowPosition(Window owner)
        {
            if (owner != null && owner.IsLoaded)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                Owner = owner;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        public void UpdateProgressMods(int? progressValue, string? ModPath)
        {
            if (_disposed) return;

            Dispatcher.Invoke(() =>
            {
                AnimateProgressBar(progressValue);
                ModPathLabel.Content = $"{Properties.Resources.LoadingWindowTitle}: {ModPath}";
            });
        }

        public async Task UpdateProgressGpt4FreeAsync(string logMessage, CancellationToken cancellationToken)
        {
            if (_disposed) return;

            var stage = ParseProgressStage(logMessage);
            if (!stage.HasValue) return;

            await UpdateProgressStageAsync(stage.Value, cancellationToken);
        }

        private static ProgressStage? ParseProgressStage(string logMessage)
        {
            if (logMessage.StartsWith("Cloning into 'gpt4free'..."))
                return ProgressStage.Cloning;

            if (logMessage.StartsWith("Collecting pip"))
                return ProgressStage.InstallingDependencies;

            if (logMessage.StartsWith("Installing collected packages"))
                return ProgressStage.InstallingPackages;

            return null;
        }

        private async Task UpdateProgressStageAsync(ProgressStage stage, CancellationToken cancellationToken)
        {
            var (start, end, delay) = stage switch
            {
                ProgressStage.Cloning => (0, 20, 700),
                ProgressStage.InstallingDependencies => (20, 70, 300),
                ProgressStage.InstallingPackages => (70, 95, 1500),
                _ => throw new InvalidEnumArgumentException("Unknown progress stage")
            };

            if (_currentProgress > start)
                return;

            if (_currentProgress < start)
                _currentProgress = start - 0.1;

            UpdateDescription(stage);
            await AnimateProgressAsync(start, end, delay, cancellationToken);
        }

        private void UpdateDescription(ProgressStage stage)
        {
            Dispatcher.Invoke(() =>
            {
                ModPathLabel.Content = _stageDescriptions[stage];
            });
        }

        private async Task AnimateProgressAsync(int start, int? end, int baseDelay, CancellationToken cancellationToken)
        {
            try
            {
                for (var i = start; i <= end; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (_currentProgress >= i)
                        continue;

                    _currentProgress = i;

                    await Dispatcher.InvokeAsync(() =>
                        AnimateProgressBar(i),
                        DispatcherPriority.Normal,
                        cancellationToken);

                    var delay = (int)(baseDelay * (RandomDelayFactor + _random.NextDouble()));
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation gracefully
            }
        }

        private void AnimateProgressBar(double? targetValue)
        {
            var animation = new DoubleAnimation
            {
                From = ProgressBar.Value,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(AnimationDurationMs),
                EasingFunction = new QuadraticEase()
            };

            ProgressBar.BeginAnimation(RangeBase.ValueProperty, animation);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _animationCts?.Cancel();
            _animationCts?.Dispose();

            Dispatcher.Invoke(Close);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private enum ProgressStage
        {
            Cloning,
            InstallingDependencies,
            InstallingPackages
        }

        public event EventHandler? CancelRequested;

        private void CancelButtonCommand(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            Close();
        }
    }
}
