using Knowte.Common.Prism;
using Prism.Events;
using System.Windows.Controls;

namespace Knowte.MainModule.Views
{
    public partial class Search : UserControl
    {
        #region Variables
        private IEventAggregator eventAggregator;
        #endregion

        #region Construction
        public Search(IEventAggregator eventAggregator)
        {
            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<SetMainSearchBoxFocusEvent>().Subscribe((x) => this.SearchBox.Focus());
        }
        #endregion
    }
}