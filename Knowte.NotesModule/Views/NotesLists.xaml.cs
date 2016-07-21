using Knowte.Common.Prism;
using Knowte.Core.Utils;
using Prism.Events;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Knowte.NotesModule.Views
{
    /// <summary>
    /// Interaction logic for NotesLists.xaml
    /// </summary>
    public partial class NotesLists : UserControl
    {
        #region Variables

        private IEventAggregator mEventAggregator;

        #endregion

        #region Properties

        public new object DataContext
        {
            get { return base.DataContext; }
            set { base.DataContext = value; }
        }

        #endregion

        #region Construction

        public NotesLists(IEventAggregator iEventAggregator)
        {
            // This call is required by the designer.
            InitializeComponent();

            this.mEventAggregator = iEventAggregator;
        }

        #endregion

        #region Private

        private void ListboxNotes_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                this.mEventAggregator.GetEvent<DeleteNoteEvent>().Publish("");
            }
        }

        private void ListBoxNotebooks_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                this.mEventAggregator.GetEvent<DeleteNotebookEvent>().Publish("");
            }
        }

        private void ListboxNotes_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // This code checks if the source of the double-click was a ListBoxItem
            // If not, we don't open the currently selected note
            ListBoxItem listBoxItem = VisualTreeUtils.FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

            if (listBoxItem == null)
            {
                return;
            }

            if (e.ChangedButton  == MouseButton.Left)
            {
                this.mEventAggregator.GetEvent<OpenNoteEvent>().Publish("");
            }
        }

        #endregion
    }
}
