using Knowte.Common.Prism;
using Knowte.Common.Utils;
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

        private IEventAggregator eventAggregator;

        #endregion

        #region Properties

        public new object DataContext
        {
            get { return base.DataContext; }
            set { base.DataContext = value; }
        }

        #endregion

        #region Construction

        public NotesLists(IEventAggregator eventAggregator)
        {
            // This call is required by the designer.
            InitializeComponent();

            this.eventAggregator = eventAggregator;
        }

        #endregion

        #region Private

        private void ListboxNotes_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                this.eventAggregator.GetEvent<DeleteNoteEvent>().Publish("");
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
                this.eventAggregator.GetEvent<OpenNoteEvent>().Publish("");
            }
        }

        #endregion
    }
}
