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
        private readonly IRegionManager regionManager;
        private IUnityContainer container;
    
        public MainModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.regionManager = regionManager;
            this.container = container;
        }
     
        public void Initialize()
        {
            this.RegisterViews();
            this.RegisterViewModels();
            this.RegisterViewsWithRegions();
        }
   
        private void RegisterViews()
        {
            this.container.RegisterType<object, MainMenu>(typeof(MainMenu).FullName);
            this.container.RegisterType<object, Search>(typeof(Search).FullName);
        }

        private void RegisterViewModels()
        {
        }

        private void RegisterViewsWithRegions()
        {
            this.regionManager.RegisterViewWithRegion(RegionNames.MainMenuRegion, typeof(Views.MainMenu));
            this.regionManager.RegisterViewWithRegion(RegionNames.SearchRegion, typeof(Views.Search));
        }
    }
}
