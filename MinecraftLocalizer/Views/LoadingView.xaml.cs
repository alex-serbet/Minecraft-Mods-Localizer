using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MinecraftLocalizer.Views
{
    public sealed partial class LoadingView : Window, IDisposable
    {
        private const int AnimationDurationMs = 300;
        private static readonly Random _random = new();


        private bool _disposed;
        private readonly CancellationTokenSource _animationCts;

        public LoadingView(Window owner)
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

        public event EventHandler? CancelRequested;

        private void CancelButtonCommand(object? sender, RoutedEventArgs e)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            Close();
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
    }
}


