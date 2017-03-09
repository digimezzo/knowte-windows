using Knowte.Common.Prism;
using Knowte.SettingsModule.ViewModels;
using Knowte.SettingsModule.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace Knowte.SettingsModule
{
    public class SettingsModule : IModule
    {
        #region Variables
        private IRegionManager regionManager;
        private IUnityContainer container;
        #endregion

        #region Construction
        public SettingsModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.regionManager = regionManager;
            this.container = container;
        }
        #endregion

        #region Public
        public void Initialize()
        {
            this.regionManager.RegisterViewWithRegion(RegionNames.SettingsRegion, typeof(Views.SettingsAppearance));

            this.container.RegisterType<object, SettingsSubMenu>(typeof(SettingsSubMenu).FullName);

            this.container.RegisterType<object, SettingsViewModel>(typeof(SettingsViewModel).FullName);
            this.container.RegisterType<object, Settings>(typeof(Settings).FullName);
            this.container.RegisterType<object, SettingsAdvancedViewModel>(typeof(SettingsAdvancedViewModel).FullName);
            this.container.RegisterType<object, SettingsAdvanced>(typeof(SettingsAdvanced).FullName);
            this.container.RegisterType<object, SettingsAppearanceViewModel>(typeof(SettingsAppearanceViewModel).FullName);
            this.container.RegisterType<object, SettingsAppearance>(typeof(SettingsAppearance).FullName);
            this.container.RegisterType<object, SettingsNotesViewModel>(typeof(SettingsNotesViewModel).FullName);
            this.container.RegisterType<object, SettingsNotes>(typeof(SettingsNotes).FullName);
        }
        #endregion
    }
}
