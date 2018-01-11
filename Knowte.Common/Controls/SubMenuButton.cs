using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Knowte.Common.Controls
{
    public class SubMenuButton : RadioButton
    {
        public string Count
        {
            get { return Convert.ToString(GetValue(CountProperty)); }

            set { SetValue(CountProperty, value); }
        }

        public bool ShowCount
        {
            get { return Convert.ToBoolean(GetValue(ShowCountProperty)); }

            set { SetValue(ShowCountProperty, value); }
        }

        public Brush AccentForeground
        {
            get { return (Brush)GetValue(AccentForegroundProperty); }

            set { SetValue(AccentForegroundProperty, value); }
        }

        public static readonly DependencyProperty CountProperty = 
            DependencyProperty.Register("Count", typeof(string), typeof(SubMenuButton), new PropertyMetadata(null));
        public static readonly DependencyProperty ShowCountProperty = 
            DependencyProperty.Register("ShowCount", typeof(bool), typeof(SubMenuButton), new PropertyMetadata(null));
        public static readonly DependencyProperty AccentForegroundProperty = 
            DependencyProperty.Register("AccentForeground", typeof(Brush), typeof(SubMenuButton), new PropertyMetadata(null));
  
        static SubMenuButton()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SubMenuButton), new FrameworkPropertyMetadata(typeof(SubMenuButton)));
        }
    }
}