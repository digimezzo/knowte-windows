using System;
using System.Windows;
using System.Windows.Controls;

namespace Knowte.Common.Controls
{
    public class FontIconAccentButton : Button
    {
        public double CornerRadius
        {
            get { return Convert.ToDouble(GetValue(CornerRadiusProperty)); }

            set { SetValue(CornerRadiusProperty, value); }
        }

        public string Glyph
        {
            get { return Convert.ToString(GetValue(GlyphProperty)); }

            set { SetValue(GlyphProperty, value); }
        }

        public double GlyphSize
        {
            get { return Convert.ToDouble(GetValue(GlyphSizeProperty)); }

            set { SetValue(GlyphSizeProperty, value); }
        }

        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register("Glyph", typeof(string), typeof(FontIconAccentButton), new PropertyMetadata(null));
        public static readonly DependencyProperty GlyphSizeProperty = DependencyProperty.Register("GlyphSize", typeof(double), typeof(FontIconAccentButton), new PropertyMetadata(null));
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(double), typeof(FontIconAccentButton), new PropertyMetadata(15.0));
      
        static FontIconAccentButton()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontIconAccentButton), new FrameworkPropertyMetadata(typeof(FontIconAccentButton)));
        }
    
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.SizeChanged += SizeChangedHandler;
        }
  
        private void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            this.CornerRadius = this.ActualHeight / 2;
        }
    }
}
