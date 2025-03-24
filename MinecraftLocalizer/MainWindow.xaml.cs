using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MinecraftLocalizer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ScrollViewer? _dataGridScrollViewer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _dataGridScrollViewer = GetScrollViewer(LocalizationDataGrid);

            if (_dataGridScrollViewer != null)
            {
                CustomScrollBar.Maximum = _dataGridScrollViewer.ScrollableHeight;
                CustomScrollBar.ViewportSize = _dataGridScrollViewer.ViewportHeight;

                _dataGridScrollViewer.ScrollChanged += (s, ev) =>
                {
                    CustomScrollBar.Maximum = _dataGridScrollViewer.ScrollableHeight;
                    CustomScrollBar.Value = _dataGridScrollViewer.VerticalOffset;
                    CustomScrollBar.ViewportSize = _dataGridScrollViewer.ViewportHeight;
                };
            }
        }

        private void CustomScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _dataGridScrollViewer?.ScrollToVerticalOffset(e.NewValue);
        }

        private static ScrollViewer? GetScrollViewer(DependencyObject parent)
        {
            if (parent is ScrollViewer sv)
                return sv;

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