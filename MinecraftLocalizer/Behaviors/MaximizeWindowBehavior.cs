using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftLocalizer.Behaviors
{
    public class MaximizeWindowBehavior : Behavior<Button>
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
            Window window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                if (window.WindowState != WindowState.Maximized)
                {

                    window.WindowState = WindowState.Maximized;
                    window.ResizeMode = ResizeMode.CanResize;
                  
                }
                else
                    window.WindowState = WindowState.Normal;
            }
        }
    }
}
