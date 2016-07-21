using Knowte.Common.Services.Search;
using Prism.Mvvm;

namespace Knowte.MainModule.ViewModels
{
    public class SearchViewModel : BindableBase
    {
        #region Variables
        private string searchText;
        private ISearchService searchService;
        #endregion

        #region Properties
        public string SearchText
        {
            get { return this.searchText; }
            set
            {
                SetProperty<string>(ref this.searchText, value);
                this.searchService.DoSearch(value);
            }
        }
        #endregion

        #region Construction
        public SearchViewModel(ISearchService searchService)
        {
            this.searchService = searchService;
        }
        #endregion
    }
}
