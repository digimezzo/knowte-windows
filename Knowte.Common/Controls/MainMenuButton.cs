using System.Windows;
using System.Windows.Controls;

namespace Knowte.Common.Controls
{
    public class MainMenuButton : RadioButton
    {
        static MainMenuButton()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MainMenuButton), new FrameworkPropertyMetadata(typeof(MainMenuButton)));
        }
    }
}