using System;
using System.Timers;

namespace Knowte.Common.Services.Search
{
    public class SearchService : ISearchService
    {
        private Timer searchTimer = new Timer();
        private double searchTimeout = 0.2;
     
        public string SearchText { get; set; }
    
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
   
        public void DoSearch(string searchText)
        {
            this.SearchText = searchText;

            this.searchTimer.Stop();
            this.searchTimer.Start();
        }
    
        public event SearchingEventHandler Searching;
    }
}