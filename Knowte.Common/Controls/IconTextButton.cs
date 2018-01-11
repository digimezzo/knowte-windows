using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Knowte.Common.Controls
{
    public class IconTextButton : Button
    {
        public Geometry Data
        {
            get { return (Geometry)GetValue(DataProperty); }

            set { SetValue(DataProperty, value); }
        }
    
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(Geometry), typeof(IconTextButton), new PropertyMetadata(null));
     
        static IconTextButton()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconTextButton), new FrameworkPropertyMetadata(typeof(IconTextButton)));
        }
    }
}