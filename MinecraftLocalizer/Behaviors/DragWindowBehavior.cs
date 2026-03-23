using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace MinecraftLocalizer.Behaviors
{
    public class DragWindowBehavior : Behavior<UIElement>
    {
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DragWindowBehavior));

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += OnMouseDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseDown -= OnMouseDown;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window window = Window.GetWindow(AssociatedObject);
                if (window != null)
                {
                    if (window.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Normal;
                        Point mousePos = e.GetPosition(window);
                        window.Left = mousePos.X - window.Width / 2;
                        window.Top = mousePos.Y - 10;
                    }
                    window.DragMove();
                }
            }

            if (Command?.CanExecute(e) == true)
            {
                Command.Execute(e);
            }
        }
    }
}
