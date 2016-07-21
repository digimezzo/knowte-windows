using System;
using System.Timers;

namespace Knowte.Common.Services.Search
{
    public class SearchService : ISearchService
    {
        #region Variables
        private Timer searchTimer = new Timer();
        private double searchTimeout = 0.2;
        #endregion

        #region Properties
        public string SearchText { get; set; }
        #endregion

        #region Construction
        public SearchService()
        {
            this.searchTimer.Interval = TimeSpan.FromSeconds(this.searchTimeout).TotalMilliseconds;
            this.searchTimer.Elapsed += (sender, e) =>
                                    {
                                        this.searchTimer.Stop();
                                        if (Searching != null)
                                        {
                                            Searching(this, null);
                                        }
                                    };
        }
        #endregion

        #region ISearchService
        public void DoSearch(string searchText)
        {
            this.SearchText = searchText;

            this.searchTimer.Stop();
            this.searchTimer.Start();
        }
        #endregion

        #region Events
        public event SearchingEventHandler Searching;
        #endregion
    }
}