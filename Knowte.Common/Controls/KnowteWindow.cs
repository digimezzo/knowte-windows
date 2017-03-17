using Digimezzo.WPFControls;
using System.Windows;
using System.Windows.Media;

namespace Knowte.Common.Controls
{
    public class KnowteWindow : BorderlessWindows8Window
    {
        #region Variables
        private bool oldTopMost;
        #endregion

        #region Properties
        public Brush Accent
        {
            get { return (Brush)GetValue(AccentProperty); }

            set { SetValue(AccentProperty, value); }
        }

        public bool IsMainWindow
        {
            get { return (bool)GetValue(IsMainWindowProperty); }

            set { SetValue(IsMainWindowProperty, value); }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty AccentProperty = DependencyProperty.Register("Accent", typeof(Brush), typeof(KnowteWindow), new PropertyMetadata(null));
        public static readonly DependencyProperty IsMainWindowProperty = DependencyProperty.Register("IsMainWindow", typeof(bool), typeof(KnowteWindow), new PropertyMetadata(null));
        #endregion

        #region Construction
        static KnowteWindow()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(KnowteWindow), new FrameworkPropertyMetadata(typeof(KnowteWindow)));
        }
        #endregion

        #region Public
        /// <summary>
        /// Custom Activate function because the real Activate function doesn't always bring the window on top.
        /// </summary>
        /// <remarks></remarks>
        public void ActivateNow()
        {
            this.oldTopMost = this.Topmost;

            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            this.Show();
            this.Activate();
            this.Topmost = true;

            System.Threading.Thread t = new System.Threading.Thread(Deactivate);
            t.Start();
        }
        #endregion

        #region Private
        /// <summary>
        /// The Deactivate function which goes together with ActivateNow
        /// </summary>
        /// <remarks></remarks>
        private void Deactivate()
        {
            System.Threading.Thread.Sleep(250);
            Application.Current.Dispatcher.Invoke(() => this.Topmost == this.oldTopMost);
        }
        #endregion
    }
}