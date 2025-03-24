using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftLocalizer.Behaviors
{
    public class CloseWindowBehavior : Behavior<Button>
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
            System.Windows.Application.Current.Shutdown();
        }
    }
}
