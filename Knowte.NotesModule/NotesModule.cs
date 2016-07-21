using Knowte.Common.Prism;
using Knowte.NotesModule.ViewModels;
using Knowte.NotesModule.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Regions;

namespace Knowte.NotesModule
{
    public class NotesModule : IModule
    {
        #region Variables
        private readonly IRegionManager regionManager;
        private IUnityContainer container;
        #endregion

        #region Construction
        public NotesModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.regionManager = regionManager;
            this.container = container;
        }
        #endregion

        #region Public
        public void Initialize()
        {
            this.regionManager.RegisterViewWithRegion(RegionNames.ContentRegion, typeof(Views.Notes));

            this.regionManager.RegisterViewWithRegion(RegionNames.SubMenuRegion, typeof(Views.NotesSubMenu));
            this.regionManager.RegisterViewWithRegion(RegionNames.NotesRegion, typeof(Views.NotesLists));

            this.container.RegisterType<object, NotesSubMenu>(typeof(NotesSubMenu).FullName);
            this.container.RegisterType<object, NotesViewModel>(typeof(NotesViewModel).FullName);
            this.container.RegisterType<object, Notes>(typeof(Notes).FullName);
            this.container.RegisterType<object, NotesListsViewModel>(typeof(NotesListsViewModel).FullName);
            this.container.RegisterType<object, NotesLists>(typeof(NotesLists).FullName);
            this.container.RegisterType<object, NoteWindow>(typeof(NoteWindow).FullName);
        }
        #endregion
    }
}