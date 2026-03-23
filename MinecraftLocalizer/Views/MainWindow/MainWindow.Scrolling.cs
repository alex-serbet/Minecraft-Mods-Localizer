using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MinecraftLocalizer.Views
{
    public partial class MainWindow
    {
        private ScrollViewer? _dataGridScrollViewer;
        private ScrollViewer? _textBoxScrollViewer;
        private ScrollViewer? _streamingScrollViewer;
        private ScrollViewer? _consoleScrollViewer;
        private bool _streamingAutoScrollEnabled = true;
        private bool _consoleAutoScrollEnabled = true;
        private bool _isSyncingStreamingScroll;
        private bool _isSyncingConsoleScroll;
        private double _lastStreamingOffset;
        private double _lastConsoleOffset;

        private void CustomDataGridScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _dataGridScrollViewer?.ScrollToVerticalOffset(e.NewValue);

        private void CustomTextBoxScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) =>
            _textBoxScrollViewer?.ScrollToVerticalOffset(e.NewValue);

        private void CustomStreamingTextScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_streamingScrollViewer == null || _isSyncingStreamingScroll)
                return;

            _isSyncingStreamingScroll = true;
            _streamingScrollViewer.ScrollToVerticalOffset(e.NewValue);
            _isSyncingStreamingScroll = false;

            _streamingAutoScrollEnabled = IsAtBottom(_streamingScrollViewer);
            if (!_streamingAutoScrollEnabled)
                _lastStreamingOffset = e.NewValue;
        }

        private void CustomConsoleTextScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_consoleScrollViewer == null || _isSyncingConsoleScroll)
                return;

            _isSyncingConsoleScroll = true;
            _consoleScrollViewer.ScrollToVerticalOffset(e.NewValue);
            _isSyncingConsoleScroll = false;

            _consoleAutoScrollEnabled = IsAtBottom(_consoleScrollViewer);
            if (!_consoleAutoScrollEnabled)
                _lastConsoleOffset = e.NewValue;
        }

        private void InitializeDataGridScrollViewer()
        {
            _dataGridScrollViewer = GetScrollViewer(LocalizationDataGrid);
            if (_dataGridScrollViewer == null)
                return;

            UpdateCustomScrollBar(_dataGridScrollViewer, CustomDataGridScrollBar);
            _dataGridScrollViewer.ScrollChanged += (_, _) =>
            {
                UpdateCustomScrollBar(_dataGridScrollViewer, CustomDataGridScrollBar);
            };
        }

        private void InitializeTextBoxScrollViewer()
        {
            _textBoxScrollViewer = GetScrollViewer(LocalizationTextBox);
            if (_textBoxScrollViewer == null)
                return;

            UpdateCustomScrollBar(_textBoxScrollViewer, CustomTextBoxScrollBar);
            _textBoxScrollViewer.ScrollChanged += (_, _) =>
            {
                UpdateCustomScrollBar(_textBoxScrollViewer, CustomTextBoxScrollBar);
            };
        }

        private void InitializeStreamingScrollViewer()
        {
            _streamingScrollViewer = GetScrollViewer(StreamingTextBox);
            if (_streamingScrollViewer == null)
                return;

            UpdateCustomScrollBar(_streamingScrollViewer, CustomStreamingTextScrollBar);
            _streamingScrollViewer.ScrollChanged += (_, e) => HandleStreamingScrollChanged(e);
        }

        private void EnsureStreamingScrollViewer()
        {
            if (!StreamingTextBox.IsVisible)
                return;

            if (_streamingScrollViewer == null)
            {
                _streamingScrollViewer = GetScrollViewer(StreamingTextBox);
                if (_streamingScrollViewer != null)
                    _streamingScrollViewer.ScrollChanged += (_, e) => HandleStreamingScrollChanged(e);
            }

            if (_streamingScrollViewer == null)
                return;

            UpdateCustomScrollBar(_streamingScrollViewer, CustomStreamingTextScrollBar);
            if (_streamingAutoScrollEnabled)
            {
                _isSyncingStreamingScroll = true;
                _streamingScrollViewer.ScrollToEnd();
                _isSyncingStreamingScroll = false;
            }
        }

        private void HandleStreamingTextChanged()
        {
            EnsureStreamingScrollViewer();
            if (_streamingScrollViewer == null)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_streamingAutoScrollEnabled)
                {
                    _isSyncingStreamingScroll = true;
                    _streamingScrollViewer.ScrollToEnd();
                    _isSyncingStreamingScroll = false;
                }

                UpdateCustomScrollBar(_streamingScrollViewer, CustomStreamingTextScrollBar);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void InitializeConsoleScrollViewer()
        {
            _consoleScrollViewer = GetScrollViewer(ConsoleTextBox);
            if (_consoleScrollViewer == null)
                return;

            UpdateCustomScrollBar(_consoleScrollViewer, CustomConsoleTextScrollBar);
            _consoleScrollViewer.ScrollChanged += (_, e) => HandleConsoleScrollChanged(e);
        }

        private void EnsureConsoleScrollViewer()
        {
            if (!ConsoleTextBox.IsVisible)
                return;

            if (_consoleScrollViewer == null)
            {
                _consoleScrollViewer = GetScrollViewer(ConsoleTextBox);
                if (_consoleScrollViewer != null)
                    _consoleScrollViewer.ScrollChanged += (_, e) => HandleConsoleScrollChanged(e);
            }

            if (_consoleScrollViewer == null)
                return;

            UpdateCustomScrollBar(_consoleScrollViewer, CustomConsoleTextScrollBar);
            if (_consoleAutoScrollEnabled)
            {
                _isSyncingConsoleScroll = true;
                _consoleScrollViewer.ScrollToEnd();
                _isSyncingConsoleScroll = false;
            }
        }

        private void HandleConsoleTextChanged()
        {
            EnsureConsoleScrollViewer();
            if (_consoleScrollViewer == null)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_consoleAutoScrollEnabled)
                {
                    _isSyncingConsoleScroll = true;
                    _consoleScrollViewer.ScrollToEnd();
                    _isSyncingConsoleScroll = false;
                }

                UpdateCustomScrollBar(_consoleScrollViewer, CustomConsoleTextScrollBar);
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void RefreshAllCustomScrollBars()
        {
            UpdateCustomScrollBar(_dataGridScrollViewer, CustomDataGridScrollBar);
            UpdateCustomScrollBar(_textBoxScrollViewer, CustomTextBoxScrollBar);
            UpdateCustomScrollBar(_streamingScrollViewer, CustomStreamingTextScrollBar);
            UpdateCustomScrollBar(_consoleScrollViewer, CustomConsoleTextScrollBar);
        }

        private static void UpdateCustomScrollBar(ScrollViewer? scrollViewer, ScrollBar? scrollBar)
        {
            if (scrollViewer == null || scrollBar == null)
                return;

            scrollBar.Maximum = scrollViewer.ScrollableHeight;
            scrollBar.ViewportSize = scrollViewer.ViewportHeight;
            scrollBar.Value = Math.Min(scrollViewer.VerticalOffset, scrollBar.Maximum);
        }

        private void HandleStreamingScrollChanged(ScrollChangedEventArgs e)
        {
            if (_streamingScrollViewer == null || _isSyncingStreamingScroll)
                return;

            if (e.ExtentHeightChange == 0)
            {
                _streamingAutoScrollEnabled = IsAtBottom(_streamingScrollViewer);
                if (!_streamingAutoScrollEnabled)
                    _lastStreamingOffset = _streamingScrollViewer.VerticalOffset;
            }
            else if (_streamingAutoScrollEnabled)
            {
                _isSyncingStreamingScroll = true;
                _streamingScrollViewer.ScrollToEnd();
                _isSyncingStreamingScroll = false;
            }
            else
            {
                _isSyncingStreamingScroll = true;
                _streamingScrollViewer.ScrollToVerticalOffset(_lastStreamingOffset);
                _isSyncingStreamingScroll = false;
            }

            UpdateCustomScrollBar(_streamingScrollViewer, CustomStreamingTextScrollBar);
        }

        private void HandleConsoleScrollChanged(ScrollChangedEventArgs e)
        {
            if (_consoleScrollViewer == null || _isSyncingConsoleScroll)
                return;

            if (e.ExtentHeightChange == 0)
            {
                _consoleAutoScrollEnabled = IsAtBottom(_consoleScrollViewer);
                if (!_consoleAutoScrollEnabled)
                    _lastConsoleOffset = _consoleScrollViewer.VerticalOffset;
            }
            else if (_consoleAutoScrollEnabled)
            {
                _isSyncingConsoleScroll = true;
                _consoleScrollViewer.ScrollToEnd();
                _isSyncingConsoleScroll = false;
            }
            else
            {
                _isSyncingConsoleScroll = true;
                _consoleScrollViewer.ScrollToVerticalOffset(_lastConsoleOffset);
                _isSyncingConsoleScroll = false;
            }

            UpdateCustomScrollBar(_consoleScrollViewer, CustomConsoleTextScrollBar);
        }

        private static bool IsAtBottom(ScrollViewer scrollViewer) =>
            scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset <= 1.0;

        private static ScrollViewer? GetScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
