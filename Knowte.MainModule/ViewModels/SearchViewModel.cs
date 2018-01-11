using Knowte.Common.Services.Search;
using Prism.Mvvm;

namespace Knowte.MainModule.ViewModels
{
    public class SearchViewModel : BindableBase
    {
        private string searchText;
        private ISearchService searchService;
   
        public string SearchText
        {
            get { return this.searchText; }
            set
            {
                SetProperty<string>(ref this.searchText, value);
                this.searchService.DoSearch(value);
            }
        }
    
        public SearchViewModel(ISearchService searchService)
        {
            this.searchService = searchService;
        }
    }
}
