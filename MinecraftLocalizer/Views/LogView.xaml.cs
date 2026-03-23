using System;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftLocalizer.Views
{
    /// <summary>
    /// Interaction logic for LogView.xaml
    /// </summary>
    public partial class LogView : Window
    {
        private ScrollViewer? _consoleScrollViewer;
        private bool _autoScrollEnabled = true;
        private bool _isSyncingScroll;
        private double _lastUserOffset;

        public LogView()
        {
            InitializeComponent();
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Initialize the scrollbar.
            InitializeScrollBar();
            AttachConsoleScrollViewer();
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Unsubscribe from events, but do not cancel installation.
            // The user just wants to close the window, not cancel installation.
            var viewModel = DataContext as ViewModels.LogViewModel;
            if (viewModel != null)
            {
                // Unsubscribe from events to avoid memory leaks.
                // The installation continues running in the background.
                try
                {
                    // Unsubscribe via reflection because the method is private.
                    // Instead, just allow the window to close.
                    // The installation continues running in the background.
                }
                catch { }
            }
        }

        private void InitializeScrollBar()
        {
            // Set initial scrollbar values.
            if (ConsoleOutputTextBox != null && CustomConsoleScrollBar != null)
            {
                // Get the text height.
                ConsoleOutputTextBox.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
                var textHeight = ConsoleOutputTextBox.DesiredSize.Height;
                var viewportHeight = ConsoleOutputTextBox.ActualHeight;

                // Configure the scrollbar.
                CustomConsoleScrollBar.Minimum = 0;
                CustomConsoleScrollBar.Maximum = Math.Max(0, textHeight - viewportHeight);
                CustomConsoleScrollBar.ViewportSize = viewportHeight;
                CustomConsoleScrollBar.Value = 0;
            }
        }

        private void AttachConsoleScrollViewer()
        {
            if (ConsoleOutputTextBox == null)
            {
                return;
            }

            _consoleScrollViewer = FindVisualChild<ScrollViewer>(ConsoleOutputTextBox);
            if (_consoleScrollViewer != null)
            {
                _consoleScrollViewer.ScrollChanged += ConsoleScrollViewer_ScrollChanged;
                UpdateScrollBarFromViewer();
            }
        }

        private void CustomConsoleScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_consoleScrollViewer != null && !_isSyncingScroll)
            {
                // Scroll the TextBox.
                _isSyncingScroll = true;
                _consoleScrollViewer.ScrollToVerticalOffset(e.NewValue);
                _isSyncingScroll = false;

                _autoScrollEnabled = IsAtBottom();
                if (!_autoScrollEnabled)
                {
                    _lastUserOffset = e.NewValue;
                }
            }
        }

        private void ConsoleOutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keep scrollbar in sync; auto-scroll is handled in ScrollChanged.
            if (_consoleScrollViewer == null || CustomConsoleScrollBar == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(UpdateScrollBarFromViewer),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ConsoleScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll || CustomConsoleScrollBar == null)
            {
                return;
            }

            if (e.ExtentHeightChange == 0)
            {
                // User scrolled.
                _autoScrollEnabled = IsAtBottom();
                if (!_autoScrollEnabled)
                {
                    _lastUserOffset = _consoleScrollViewer?.VerticalOffset ?? 0;
                }
            }
            else
            {
                // Content changed.
                if (_autoScrollEnabled)
                {
                    _isSyncingScroll = true;
                    _consoleScrollViewer?.ScrollToEnd();
                    _isSyncingScroll = false;
                }
                else
                {
                    _isSyncingScroll = true;
                    _consoleScrollViewer?.ScrollToVerticalOffset(_lastUserOffset);
                    _isSyncingScroll = false;
                }
            }

            UpdateScrollBarFromViewer();
        }

        private void UpdateScrollBarFromViewer()
        {
            if (_consoleScrollViewer == null || CustomConsoleScrollBar == null)
            {
                return;
            }

            _isSyncingScroll = true;
            CustomConsoleScrollBar.Minimum = 0;
            CustomConsoleScrollBar.Maximum = Math.Max(0, _consoleScrollViewer.ScrollableHeight);
            CustomConsoleScrollBar.ViewportSize = _consoleScrollViewer.ViewportHeight;
            CustomConsoleScrollBar.Value = _consoleScrollViewer.VerticalOffset;
            _isSyncingScroll = false;
        }

        private bool IsAtBottom()
        {
            if (_consoleScrollViewer == null)
            {
                return true;
            }

            return _consoleScrollViewer.ScrollableHeight - _consoleScrollViewer.VerticalOffset <= 1.0;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}


