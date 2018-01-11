using System.Windows;
using System.Windows.Input;

namespace Knowte.Common.Controls
{
    public class DropUpButton : AccentButton
    {
        private bool isContextMenuOpen = false;
       
        public DropUpButton() : base()
        {
            base.Click += MyClick;
            base.MouseRightButtonUp += MyMouseRightButtonUp;
        }
      
        protected void MyClick(object sender, RoutedEventArgs e)
        {
            if (!this.isContextMenuOpen)
            {
                ContextMenu.IsEnabled = true;
                ContextMenu.PlacementTarget = this;
                ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;

                ContextMenu.IsOpen = true;

                ContextMenu.HorizontalOffset = -(ContextMenu.ActualWidth / 2 - this.ActualWidth / 2);
                this.isContextMenuOpen = true;

                ContextMenu.Closed += ContextMenu_Closed;
            }
            else
            {
                this.isContextMenuOpen = false;
                ContextMenu.Closed -= ContextMenu_Closed;
            }
        }

        protected void MyMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            this.isContextMenuOpen = false;
        }
    }
}