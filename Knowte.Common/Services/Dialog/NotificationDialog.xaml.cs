using Digimezzo.WPFControls;
using Knowte.Core.Base;
using Knowte.Core.Extensions;
using Knowte.Core.IO;
using Knowte.Core.Logging;
using System;
using System.Windows;

namespace Knowte.Common.Services.Dialog
{
    public partial class NotificationDialog : BorderlessWindows8Window
    {
        #region Construction
        public NotificationDialog(Window parent, int iconCharCode, int iconSize, string title, string content, string okText, bool showViewLogs, string viewLogsText) : base()
        {
            InitializeComponent();

            this.TitleBarHeight = Defaults.DefaultWindowButtonHeight + 10;
            this.Icon.Text = char.ConvertFromUtf32(iconCharCode);
            this.Icon.FontSize = iconSize;
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
                Actions.TryViewInExplorer(LogClient.Instance.LogFile);
            }
            catch (Exception ex)
            {
                LogClient.Instance.Logger.Error("Could not view the log file {0} in explorer. Exception: {1}", LogClient.Instance.LogFile, ex.Message);
            }
        }
        #endregion
    }
}