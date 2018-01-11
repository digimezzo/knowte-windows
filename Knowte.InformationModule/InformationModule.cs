using Knowte.Common.Prism;
using Knowte.InformationModule.ViewModels;
using Knowte.InformationModule.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace Knowte.InformationModule
{
    public class InformationModule : IModule
    {
        private IRegionManager regionManager;
        private IUnityContainer container;
    
        public InformationModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.regionManager = regionManager;
            this.container = container;
        }
     
        public void Initialize()
        {
            this.regionManager.RegisterViewWithRegion(RegionNames.InformationRegion, typeof(Views.InformationAbout));

            this.container.RegisterType<object, InformationSubMenu>(typeof(InformationSubMenu).FullName);
            this.container.RegisterType<object, InformationViewModel>(typeof(InformationViewModel).FullName);
            this.container.RegisterType<object, Information>(typeof(Information).FullName);
            this.container.RegisterType<object, InformationAboutViewModel>(typeof(InformationAboutViewModel).FullName);
            this.container.RegisterType<object, InformationAbout>(typeof(InformationAbout).FullName);
            this.container.RegisterType<object, InformationAboutLicense>(typeof(InformationAboutLicense).FullName);
        }
    }
}