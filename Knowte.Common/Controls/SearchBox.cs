using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Knowte.Common.Controls
{
    public class SearchBox : TextBox
    {
        private FontIcon searchIconCross;
        private FontIcon searchIconGlass;
        private Border searchBorder;
     
        public bool HasText
        {
            get { return Convert.ToBoolean(GetValue(HasTextProperty)); }

            set { SetValue(HasTextProperty, value); }
        }

        public bool HasFocus
        {
            get { return Convert.ToBoolean(GetValue(HasFocusProperty)); }

            set { SetValue(HasFocusProperty, value); }
        }

        public Brush Accent
        {
            get { return (Brush)GetValue(AccentProperty); }

            set { SetValue(AccentProperty, value); }
        }

        public Brush SearchGlassForeground
        {
            get { return (Brush)GetValue(SearchGlassForegroundProperty); }

            set { SetValue(SearchGlassForegroundProperty, value); }
        }

        public Brush VisibleBackground
        {
            get { return (Brush)GetValue(VisibleBackgroundProperty); }

            set { SetValue(VisibleBackgroundProperty, value); }
        }
      
        public static readonly DependencyProperty HasTextProperty = 
            DependencyProperty.Register("HasText", typeof(bool), typeof(SearchBox), new PropertyMetadata(false));
        public static readonly DependencyProperty HasFocusProperty = 
            DependencyProperty.Register("HasFocus", typeof(bool), typeof(SearchBox), new PropertyMetadata(false));
        public static readonly DependencyProperty AccentProperty = 
            DependencyProperty.Register("Accent", typeof(Brush), typeof(SearchBox), new PropertyMetadata(null));
        public static readonly DependencyProperty SearchGlassForegroundProperty = 
            DependencyProperty.Register("SearchGlassForeground", typeof(Brush), typeof(SearchBox), new PropertyMetadata(null));
        public static readonly DependencyProperty VisibleBackgroundProperty = 
            DependencyProperty.Register("VisibleBackground", typeof(Brush), typeof(SearchBox), new PropertyMetadata(null));
     
        static SearchBox()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBox), new FrameworkPropertyMetadata(typeof(SearchBox)));
        }
   
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.searchIconCross = (FontIcon)GetTemplateChild("PART_SearchIconCross");
            this.searchIconGlass = (FontIcon)GetTemplateChild("PART_SearchIconGlass");
            this.searchBorder = (Border)GetTemplateChild("PART_SearchBorder");

            if (this.searchBorder != null)
            {
                this.searchBorder.MouseLeftButtonUp += SearchButton_MouseLeftButtonUp;
            }

            this.SetButtonState();
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            this.HasText = Text.Length > 0;
            this.SetButtonState();
        }
        
        private void SetButtonState()
        {

            if (this.searchIconCross != null && this.searchIconGlass != null)
            {
                if (this.HasText)
                {
                    this.searchIconCross.Visibility = Visibility.Visible;
                    this.searchIconGlass.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.searchIconCross.Visibility = Visibility.Collapsed;
                    this.searchIconGlass.Visibility = Visibility.Visible;
                }
            }
        }
   
        private void SearchButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.HasText)
            {
                this.Text = string.Empty;
            }
            this.SetButtonState();
        }
    }
}