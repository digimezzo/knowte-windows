using System;
using System.Windows;
using System.Windows.Controls;

namespace Knowte.Common.Controls
{
    public class FontIcon : Label
    {
      
        public string Glyph
        {
            get { return Convert.ToString(GetValue(GlyphProperty)); }

            set { SetValue(GlyphProperty, value); }
        }
     
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register("Glyph", typeof(string), typeof(FontIcon), new PropertyMetadata(null));
   
        static FontIcon()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FontIcon), new FrameworkPropertyMetadata(typeof(FontIcon)));
        }
    }
}