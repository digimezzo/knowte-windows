using Knowte.Common.Presentation.Views;
using Knowte.Common.Prism;
using Knowte.MainModule.ViewModels;
using Knowte.MainModule.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace Knowte.MainModule
{
    public class MainModule : IModule
    {
        #region Variables
        private readonly IRegionManager regionManager;
        private IUnityContainer container;
        #endregion

        #region Construction
        public MainModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.regionManager = regionManager;
            this.container = container;
        }
        #endregion

        #region Public
        public void Initialize()
        {
            this.RegisterViews();
            this.RegisterViewModels();
            this.RegisterViewsWithRegions();
        }
        #endregion

        #region Private
        private void RegisterViews()
        {
            this.container.RegisterType<object, MainMenu>(typeof(MainMenu).FullName);
            this.container.RegisterType<object, Search>(typeof(Search).FullName);
        }

        private void RegisterViewModels()
        {
            this.container.RegisterType<object, MainMenuViewModel>(typeof(MainMenuViewModel).FullName);
        }

        private void RegisterViewsWithRegions()
        {
            this.regionManager.RegisterViewWithRegion(RegionNames.MainMenuRegion, typeof(Views.MainMenu));
            this.regionManager.RegisterViewWithRegion(RegionNames.SearchRegion, typeof(Views.Search));
        }
        #endregion
    }

}
