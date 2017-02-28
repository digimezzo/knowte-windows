using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Knowte.Common.Services.Dialog
{
    public delegate void DialogVisibleChangedEventHandler(bool isDialogVisible);

    public interface IDialogService
    {
        bool ShowBusyDialog(Window parent, string title, string content, int delayMilliseconds, Func<Task<bool>> callback);
        bool ShowConfirmationDialog(Window parent, int iconCharCode, int iconSize, string title, string content, string okText, string cancelText);
        bool ShowNotificationDialog(Window parent, int iconCharCode, int iconSize, string title, string content, string okText, bool showViewLogs, string viewLogsText = "Log file");
        bool ShowCustomDialog(Window parent, int iconCharCode, int iconSize, string title, UserControl content, int width, int height, bool canResize, bool showCancelButton, string okText, string cancelText, Func<Task<bool>> callback);
        bool ShowInputDialog(Window parent, int iconCharCode, int iconSize, string title, string content, string okText, string cancelText, ref string responeText);
        event DialogVisibleChangedEventHandler DialogVisibleChanged;
    }
}