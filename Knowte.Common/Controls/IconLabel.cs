using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Knowte.Common.Controls
{
    public class IconLabel : Label
    {
        #region Properties
        public Brush IconColor
        {
            get { return (Brush)GetValue(IconColorProperty); }

            set
            {
                SetValue(IconColorProperty, value);
                Debug.WriteLine(value);
            }
        }

        public Brush BorderColor
        {
            get { return (Brush)GetValue(BorderColorProperty); }

            set
            {
                SetValue(BorderColorProperty, value);
                Debug.WriteLine(value);
            }
        }

        public Brush BackgroundColor
        {
            get { return (Brush)GetValue(BackgroundColorProperty); }

            set
            {
                SetValue(BackgroundColorProperty, value);
                Debug.WriteLine(value);
            }
        }



        public Brush TextColor
        {
            get { return (Brush)GetValue(TextColorProperty); }

            set
            {
                SetValue(TextColorProperty, value);
                Debug.WriteLine(value);
            }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty IconColorProperty = DependencyProperty.Register("IconColor", typeof(Brush), typeof(IconLabel), new PropertyMetadata(null));
        public static readonly DependencyProperty BorderColorProperty = DependencyProperty.Register("BorderColor", typeof(Brush), typeof(IconLabel), new PropertyMetadata(null));
        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(IconLabel), new PropertyMetadata(null));
        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register("TextColor", typeof(Brush), typeof(IconLabel), new PropertyMetadata(null));
        #endregion
    }
}