using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Log;
using Digimezzo.WPFControls;
using Knowte.Common.Base;
using Knowte.Common.Extensions;
using Knowte.Common.IO;
using System;
using System.Windows;

namespace Knowte.Common.Services.Dialog
{
    public partial class NotificationDialog : BorderlessWindows8Window
    {
        #region Construction
        public NotificationDialog(Window parent, string title, string content, string okText, bool showViewLogs, string viewLogsText) : base()
        {
            InitializeComponent();

            this.TitleBarHeight = Defaults.DefaultWindowButtonHeight + 10;
            this.Title = title;
            this.TextBlockTitle.Text = title;
            this.TextBlockContent.Text = content;
            this.ButtonOK.Content = okText;
            this.ButtonViewLogs.Content = viewLogsText;

            if (showViewLogs)
            {
                this.ButtonViewLogs.Visibility = Visibility.Visible;
            }
            else
            {
                this.ButtonViewLogs.Visibility = Visibility.Collapsed;
            }

            this.CenterWindow(parent);
        }
        #endregion

        #region Event Handlers
        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void ButtonViewLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Actions.TryViewInExplorer(LogClient.Logfile());
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not view the log file {0} in explorer. Exception: {1}", LogClient.Logfile(), ex.Message);
            }
        }
        #endregion
    }
}