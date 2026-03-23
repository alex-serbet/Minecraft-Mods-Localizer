using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace MinecraftLocalizer.Behaviors
{
    public class ToggleCollapseBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty SectionProperty =
            DependencyProperty.Register("Section", typeof(string), typeof(ToggleCollapseBehavior), new PropertyMetadata(""));

        public string Section
        {
            get { return (string)GetValue(SectionProperty); }
            set { SetValue(SectionProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.Cursor = Cursors.Hand;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.Cursor = Cursors.Arrow;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.DataContext is ViewModels.SettingsViewModel viewModel)
            {
                switch (Section)
                {
                    case "Gpt4Free":
                        viewModel.IsGpt4FreeCollapsed = !viewModel.IsGpt4FreeCollapsed;
                        break;
                    case "DeepSeek":
                        viewModel.IsDeepSeekCollapsed = !viewModel.IsDeepSeekCollapsed;
                        break;
                    case "MainSettings":
                        viewModel.IsMainSettingsCollapsed = !viewModel.IsMainSettingsCollapsed;
                        break;
                    case "Prompt":
                        viewModel.IsPromptCollapsed = !viewModel.IsPromptCollapsed;
                        break;
                }
            }
        }
    }
}