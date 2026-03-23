using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MinecraftLocalizer.Behaviors
{
    public static class CheckBoxTriStateBehavior
    {
        public static readonly DependencyProperty DisableIndeterminateToggleProperty =
            DependencyProperty.RegisterAttached(
                "DisableIndeterminateToggle",
                typeof(bool),
                typeof(CheckBoxTriStateBehavior),
                new PropertyMetadata(false, OnDisableIndeterminateToggleChanged));

        public static bool GetDisableIndeterminateToggle(DependencyObject obj) =>
            (bool)obj.GetValue(DisableIndeterminateToggleProperty);

        public static void SetDisableIndeterminateToggle(DependencyObject obj, bool value) =>
            obj.SetValue(DisableIndeterminateToggleProperty, value);

        private static void OnDisableIndeterminateToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CheckBox checkBox)
                return;

            if (e.NewValue is true)
            {
                checkBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                checkBox.PreviewKeyDown += OnPreviewKeyDown;
            }
            else
            {
                checkBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                checkBox.PreviewKeyDown -= OnPreviewKeyDown;
            }
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not CheckBox checkBox || !checkBox.IsThreeState)
                return;

            ToggleWithoutIndeterminate(checkBox);
            e.Handled = true;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space)
                return;

            if (sender is not CheckBox checkBox || !checkBox.IsThreeState)
                return;

            ToggleWithoutIndeterminate(checkBox);
            e.Handled = true;
        }

        private static void ToggleWithoutIndeterminate(CheckBox checkBox)
        {
            checkBox.IsChecked = checkBox.IsChecked == true ? false : true;
        }
    }
}
