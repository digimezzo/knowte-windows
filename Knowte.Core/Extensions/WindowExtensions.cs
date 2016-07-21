using System;
using System.Windows;

namespace Knowte.Core.Extensions
{
    public static class WindowExtensions
    {
        public static void SetGeometry(this Window win, double top, double left, double width, double height, double topFallback = 50, double leftFallback = 50)
        {
            double totalScreenWidth = SystemParameters.VirtualScreenWidth;
            double totalScreenHeight = SystemParameters.VirtualScreenHeight;
            double totalScreenTop = SystemParameters.VirtualScreenTop;
            double totalScreenLeft = SystemParameters.VirtualScreenLeft;

            if (top < totalScreenTop | top > totalScreenHeight | left < totalScreenLeft | left > totalScreenWidth)
            {
                top = Convert.ToInt32(topFallback);
                left = Convert.ToInt32(leftFallback);
            }

            win.Top = top;
            win.Left = left;
            win.Width = width;
            win.Height = height;
        }

        public static void CenterWindow(this Window win, Window parent)
        {
            try
            {
                if (parent != null)
                {
                    // If there is a parent, center on the parent
                    win.Owner = parent;
                    win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                }
                else if (Application.Current.MainWindow.IsVisible)
                {
                    // If there is no parent, try to center on the main window if it is visible. 
                    win.Owner = Application.Current.MainWindow;
                    win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // If the main window is not visible, center on the screen.
                    win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                }
            }
            catch (Exception)
            {
                // The Try-Catch should not be necessary. But added just in case.
                win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            }
        }
    }
}
