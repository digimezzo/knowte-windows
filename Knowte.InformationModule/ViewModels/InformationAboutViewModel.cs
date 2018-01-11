using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Packaging;
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
        private IUnityContainer container;
        private IDialogService dialogService;
        private Package package;
     
        public DelegateCommand ShowLicenseCommand { get; set; }
      
        public Package Package
        {
            get { return this.package; }
            set { SetProperty<Package>(ref this.package, value); }
        }
      
        public InformationAboutViewModel(IUnityContainer container, IDialogService dialogService)
        {
            this.container = container;
            this.dialogService = dialogService;

            this.Package = new Package(ProcessExecutable.Name(), ProcessExecutable.AssemblyVersion());
            this.ShowLicenseCommand = new DelegateCommand(() => this.ShowLicense());
        }

        private void ShowLicense()
        {
            var view = this.container.Resolve<InformationAboutLicense>();

            this.dialogService.ShowCustomDialog(
                null,
                ResourceUtils.GetString("Language_License"),
                view,
                400,
                0,
                false,
                false,
                ResourceUtils.GetString("Language_Ok"),
                string.Empty,
                null);
        }
    }
}
