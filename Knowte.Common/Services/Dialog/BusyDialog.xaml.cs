using Digimezzo.WPFControls;
using System.Windows;
using Knowte.Common.Extensions;
using System.Threading.Tasks;
using System;

namespace Knowte.Common.Services.Dialog
{
    public partial class BusyDialog : BorderlessWindows8Window
    {
        #region Variables
        private int delayMilliseconds;
        private Func<Task<bool>> callback;
        #endregion

        #region Construction
        public BusyDialog(Window parent, string title, string content, int delayMilliseconds, Func<Task<bool>> callback) : base()
        {
            InitializeComponent();
            this.delayMilliseconds = delayMilliseconds;
            this.callback = callback;
            this.Title = title;
            this.Content.Text = content;
            this.CenterWindow(parent);
        }
        #endregion

        #region Event handlers
        private async void BusyDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.callback != null)
            {
                await Task.Delay(delayMilliseconds);
                this.DialogResult = await this.callback.Invoke();
            }
            else
            {
                this.DialogResult = false;
            }
        }
        #endregion
    }
}
