using Knowte.Common.Prism;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System.Linq;

namespace Knowte.MainModule.ViewModels
{
    public class MainMenuViewModel : BindableBase
    {
        #region Variables
        private string activeSettingsView = typeof(SettingsModule.Views.SettingsAppearance).FullName; // Default active Settings View
        private string activeInformationView = typeof(InformationModule.Views.InformationAbout).FullName; // Default active Information View
        #endregion

        #region Construction
        public MainMenuViewModel()
        {
        }
        #endregion
    }
}