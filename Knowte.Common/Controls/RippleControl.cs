using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Knowte.Common.Controls
{
    public class RippleControl : ContentControl
    {
        Ellipse ellipse;
        Grid grid;
        Storyboard animation;

        public static readonly DependencyProperty RippleBackgroundProperty =
            DependencyProperty.Register("RippleBackground", typeof(Brush), typeof(RippleControl), new PropertyMetadata(Brushes.White));

        public static readonly DependencyProperty AnimateProperty =
            DependencyProperty.Register("Animate", typeof(bool), typeof(RippleControl), new PropertyMetadata(AnimateChangedHandler));

        public Brush RippleBackground
        {
            get { return (Brush)GetValue(RippleBackgroundProperty); }
            set { SetValue(RippleBackgroundProperty, value); }
        }

        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        static RippleControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RippleControl), new FrameworkPropertyMetadata(typeof(RippleControl)));
        }

        private static void AnimateChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (RippleControl)d;

            if (!self.Animate) return; // Only animate if true

            var targetWidth = Math.Max(self.ActualWidth, self.ActualHeight) * 2;
            var mousePosition = Mouse.GetPosition(self);
            var startMargin = new Thickness(mousePosition.X, mousePosition.Y, 0, 0);

            // Set initial margin to mouse position
            self.ellipse.Margin = startMargin;

            // Set the "To" value of the animation that animates the width to the target width
            (self.animation.Children[0] as DoubleAnimation).To = targetWidth;

            // Set the "To" and "From" values of the animation that animates the distance relative to the container (grid)
            (self.animation.Children[1] as ThicknessAnimation).From = startMargin;
            (self.animation.Children[1] as ThicknessAnimation).To = new Thickness(mousePosition.X - targetWidth / 2, mousePosition.Y - targetWidth / 2, 0, 0);
            self.ellipse.BeginStoryboard(self.animation);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ellipse = GetTemplateChild("PART_ellipse") as Ellipse;
            grid = GetTemplateChild("PART_grid") as Grid;
            animation = grid.FindResource("PART_animation") as Storyboard;
        }
    }
}
