using Microsoft.Xaml.Behaviors;
using MinecraftLocalizer.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MinecraftLocalizer.Behaviors
{
    public class DataGridRowClickSelectBehavior : Behavior<DataGrid>
    {
        private DataGridRow? _lastToggledRow;

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(DataGridRowClickSelectBehavior), new PropertyMetadata(false));

        public bool IsDragEnabled
        {
            get => (bool)GetValue(IsDragEnabledProperty);
            set => SetValue(IsDragEnabledProperty, value);
        }

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.Register(nameof(IsDragEnabled), typeof(bool), typeof(DataGridRowClickSelectBehavior), new PropertyMetadata(false));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled)
                return;

            if (e.OriginalSource is not DependencyObject source)
                return;

            if (FindParent<CheckBox>(source) != null)
                return;

            var row = FindParent<DataGridRow>(source);
            if (row?.Item is LocalizationItem item)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragEnabled)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _lastToggledRow = null;
                return;
            }

            if (e.OriginalSource is not DependencyObject source)
                return;

            var row = FindParent<DataGridRow>(source);
            if (row == null || row == _lastToggledRow)
                return;

            _lastToggledRow = row;

            if (row.Item is LocalizationItem item)
            {
                item.IsSelected = !item.IsSelected;
            }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? current = child;
            while (current != null)
            {
                if (current is T parent)
                    return parent;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
