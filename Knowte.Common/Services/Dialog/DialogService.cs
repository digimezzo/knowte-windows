using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Knowte.Common.Services.Dialog
{
    public class DialogService : IDialogService
    {
        #region Variables
        private int openDialogCount = 0;
        #endregion

        #region Private
        private void ShowDialog(Window window)
        {
            this.openDialogCount += 1;
            this.DialogVisibleChanged(this.openDialogCount > 0);
            window.ShowDialog();
            this.openDialogCount -= 1;
            this.DialogVisibleChanged(this.openDialogCount > 0);
        }
        #endregion

        #region IDialogService
        public bool ShowBusyDialog(Window parent, string title, string content, int delayMilliseconds, Func<Task<bool>> callback)
        {
            var dialog = new BusyDialog(parent: parent, title: title, content: content, delayMilliseconds: delayMilliseconds, callback: callback);

            this.ShowDialog(dialog);

            return dialog.DialogResult.HasValue & dialog.DialogResult.Value;
        }

        public bool ShowConfirmationDialog(Window parent, string title, string content, string okText, string cancelText)
        {
            var dialog = new ConfirmationDialog(parent: parent, title: title, content: content, okText: okText, cancelText: cancelText);

            this.ShowDialog(dialog);

            return dialog.DialogResult.HasValue & dialog.DialogResult.Value;
        }

        public bool ShowNotificationDialog(Window parent, string title, string content, string okText, bool showViewLogs, string viewLogsText = "Log file")
        {
            var dialog = new NotificationDialog(parent: parent, title: title, content: content, okText: okText, showViewLogs: showViewLogs, viewLogsText: viewLogsText);

            this.ShowDialog(dialog);

            // Always return True when a Notification is shown
            return true;
        }

        public bool ShowCustomDialog(Window parent, string title, UserControl content, int width, int height, bool canResize, bool sShowCancelButton, string okText, string cancelText, Func<Task<bool>> callback)
        {
            var dialog = new CustomDialog(parent: parent, title: title, content: content, width: width, height: height, canResize: canResize, showCancelButton: sShowCancelButton, okText: okText, cancelText: cancelText, callback: callback);

            this.ShowDialog(dialog);

            return dialog.DialogResult.HasValue & dialog.DialogResult.Value;
        }

        public bool ShowInputDialog(Window parent, string title, string content, string okText, string cancelText, ref string responeText)
        {
            var dialog = new InputDialog(parent: parent, title: title, content: content, okText: okText, cancelText: cancelText, defaultResponse: responeText);

            this.ShowDialog(dialog);

            if (dialog.DialogResult.HasValue & dialog.DialogResult.Value)
            {
                responeText = ((InputDialog)dialog).ResponseText;
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Events
        public event DialogVisibleChangedEventHandler DialogVisibleChanged = delegate { };
        #endregion
    }
}