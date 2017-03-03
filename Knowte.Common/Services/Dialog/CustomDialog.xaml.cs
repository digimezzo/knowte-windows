using Digimezzo.WPFControls;
using Knowte.Common.Base;
using Knowte.Common.Extensions;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Knowte.Common.Services.Dialog
{
    /// <summary>
    /// Interaction logic for CustomDialog.xaml
    /// </summary>
    public partial class CustomDialog : BorderlessWindows8Window
    {
        #region Variables
        private Func<Task<bool>> callback;
        #endregion

        #region Construction
        public CustomDialog(Window parent, string title, UserControl content, int width, int height, bool canResize, bool showCancelButton, string okText, string cancelText, Func<Task<bool>> callback) : base()
        {
            InitializeComponent();

            this.TitleBarHeight = Defaults.DefaultWindowButtonHeight + 10;
            this.Title = title;
            this.TextBlockTitle.Text = title;
            this.Width = width;
            this.MinWidth = width;
            this.CustomContent.Content = content;

            if (canResize)
            {
                this.ResizeMode = ResizeMode.CanResize;
                this.Height = height;
                this.MinHeight = height;
                this.SizeToContent = SizeToContent.Manual;
            }
            else
            {
                this.ResizeMode = ResizeMode.NoResize;
                this.SizeToContent = SizeToContent.Height;
            }

            if (showCancelButton)
            {
                this.ButtonCancel.Visibility = Visibility.Visible;
            }
            else
            {
                this.ButtonCancel.Visibility = Visibility.Collapsed;
            }

            this.ButtonOK.Content = okText;
            this.ButtonCancel.Content = cancelText;

            this.callback = callback;

            this.CenterWindow(parent);
        }
        #endregion

        #region Event Handlers
        private async void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (this.callback != null)
            {
                // Execute some function in the caller of this dialog.
                // If the result is False, DialogResult is not set.
                // That keeps the dialog open.
                if (await this.callback.Invoke())
                {
                    this.DialogResult = true;
                }
            }
            else
            {
                this.DialogResult = true;
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
        #endregion
    }
}
