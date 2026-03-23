using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MinecraftLocalizer.Views
{
    public partial class MainWindow
    {
        private DateTime _lastContextMenuOpenUtc = DateTime.MinValue;
        private DateTime _lastTreeViewContextMenuOpenUtc = DateTime.MinValue;
        private bool _isDataGridContextMenuOpen;
        private bool _isTreeViewContextMenuOpen;

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        private void TreeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_isTreeViewContextMenuOpen)
            {
                e.Handled = true;
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastTreeViewContextMenuOpenUtc).TotalMilliseconds < 150)
            {
                e.Handled = true;
                return;
            }

            _lastTreeViewContextMenuOpenUtc = now;
        }

        private void TreeView_ContextMenuOpened(object sender, RoutedEventArgs e) => _isTreeViewContextMenuOpen = true;

        private void TreeView_ContextMenuClosed(object sender, RoutedEventArgs e) => _isTreeViewContextMenuOpen = false;

        private void LocalizationDataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_isDataGridContextMenuOpen)
            {
                e.Handled = true;
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastContextMenuOpenUtc).TotalMilliseconds < 150)
            {
                e.Handled = true;
                return;
            }

            _lastContextMenuOpenUtc = now;

            if (sender is not DataGrid grid)
                return;

            var hit = grid.InputHitTest(Mouse.GetPosition(grid)) as DependencyObject;
            var cell = hit != null ? FindParent<DataGridCell>(hit) : null;
            if (cell?.DataContext != null && cell.Column != null)
            {
                grid.SelectedItem = cell.DataContext;
                grid.CurrentCell = new DataGridCellInfo(cell.DataContext, cell.Column);
            }
        }

        private void LocalizationDataGrid_ContextMenuOpened(object sender, RoutedEventArgs e) => _isDataGridContextMenuOpen = true;

        private void LocalizationDataGrid_ContextMenuClosed(object sender, RoutedEventArgs e) => _isDataGridContextMenuOpen = false;

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (current is T typed)
                    return typed;

                current = current is Visual or Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
