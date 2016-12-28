using Knowte.Common.Services.Dialog;
using Knowte.Common.Utils;
using Knowte.InformationModule.Views;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Mvvm;

namespace Knowte.InformationModule.ViewModels
{
    public class InformationAboutViewModel : BindableBase
    {
        #region Private
        private IUnityContainer container;
        private IDialogService dialogService;
        #endregion

        #region Commands
        public DelegateCommand ShowLicenseCommand { get; set; }
        #endregion

        #region Construction
        public InformationAboutViewModel(IUnityContainer container, IDialogService dialogService)
        {
            this.container = container;
            this.dialogService = dialogService;

            this.ShowLicenseCommand = new DelegateCommand(() => {

                var view = this.container.Resolve<InformationAboutLicense>();

                this.dialogService.ShowCustomDialog(
                    null,
                    0xe73e, 
                    16, 
                    ResourceUtils.GetStringResource("Language_License"), 
                    view, 
                    400, 
                    0, 
                    false, 
                    false, 
                    ResourceUtils.GetStringResource("Language_Ok"), 
                    string.Empty, 
                    null);
            });
        }
        #endregion
    }
}
