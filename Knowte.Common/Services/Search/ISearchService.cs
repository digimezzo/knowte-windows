using System;

namespace Knowte.Common.Services.Search
{
    public delegate void SearchingEventHandler(object sender, EventArgs e);

    public interface ISearchService
    {
        string SearchText { get; set; }
        void DoSearch(string searchText);
        event SearchingEventHandler Searching;
    }
}