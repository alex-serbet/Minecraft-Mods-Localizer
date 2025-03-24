using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows;

namespace MinecraftLocalizer.Models.Utils
{
    public class ProgressIconHelper
    {
        public static bool GetIsAnimating(DependencyObject obj) => (bool)obj.GetValue(IsAnimatingProperty);
        public static void SetIsAnimating(DependencyObject obj, bool value) => obj.SetValue(IsAnimatingProperty, value);

        public static readonly DependencyProperty IsAnimatingProperty =
            DependencyProperty.RegisterAttached("IsAnimating", typeof(bool), typeof(ProgressIconHelper),
                new PropertyMetadata(false, OnIsAnimatingChanged));

        private static void OnIsAnimatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element && element.Resources["RotationStoryboard"] is Storyboard storyboard)
            {
                if ((bool)e.NewValue) storyboard.Begin();
                else storyboard.Stop();
            }
        }
    }
}
