using Digimezzo.Utilities.Settings;
using Knowte.Common.Prism;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace Knowte.SettingsModule.ViewModels
{
    public class SettingsAdvancedViewModel : BindableBase
    {
        #region Variables
        private IEventAggregator eventAggregator;
        private ObservableCollection<int> numberOfNotesInJumpList;
        private int selectedNumberOfNotesInJumpList;
        #endregion

        #region Properties
        public ObservableCollection<int> NumberOfNotesInJumpList
        {
            get
            {
                return this.numberOfNotesInJumpList;
            }
            set
            {
                SetProperty<ObservableCollection<int>>(ref this.numberOfNotesInJumpList, value);
            }
        }

        public int SelectedNumberOfNotesInJumpList
        {
            get
            {
                return this.selectedNumberOfNotesInJumpList;
            }
            set
            {
                SetProperty<int>(ref this.selectedNumberOfNotesInJumpList, value);
                SettingsClient.Set<int>("Advanced", "NumberOfNotesInJumpList", value);
                this.eventAggregator.GetEvent<RefreshJumpListEvent>().Publish("");
            }
        }

        #endregion

        #region Construction
        public SettingsAdvancedViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            this.LoadNumberOfNotesInJumplist();
        }
        #endregion

        #region Private
        private void LoadNumberOfNotesInJumplist()
        {
            this.numberOfNotesInJumpList = new ObservableCollection<int>();

            for (int i = 0; i <= 10; i++)
            {
                this.NumberOfNotesInJumpList.Add(i);
            }

            this.SelectedNumberOfNotesInJumpList = SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList");
        }
        #endregion
    }
}