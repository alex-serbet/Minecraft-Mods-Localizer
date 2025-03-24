using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftLocalizer.Behaviors
{
    public class MinimizeWindowBehavior : Behavior<Button>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.Click += OnClick;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.Click -= OnClick;
            }
            base.OnDetaching();
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }
}
