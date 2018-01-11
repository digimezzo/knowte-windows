using Knowte.Common.Prism;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;

namespace Knowte.InformationModule.ViewModels
{
    public class InformationViewModel : BindableBase
    {
        private IRegionManager regionManager;
        private int slideInFrom;
        private int previousIndex;
     
        public DelegateCommand<string> NavigateBetweenInformationCommand;
      
        public int SlideInFrom
        {
            get { return this.slideInFrom; }
            set { SetProperty<int>(ref this.slideInFrom, value); }
        }
       
        public InformationViewModel(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
            this.NavigateBetweenInformationCommand = new DelegateCommand<string>((index) => this.NavigateBetweenInformation(index));
            ApplicationCommands.NavigateBetweenInformationCommand.RegisterCommand(this.NavigateBetweenInformationCommand);

            this.SlideInFrom = 50;
        }
       
        private void NavigateBetweenInformation(string index)
        {
            if (string.IsNullOrWhiteSpace(index)) return;

            int localIndex = 0;
            int.TryParse(index, out localIndex);

            if (localIndex == 0) return;

            this.SlideInFrom = localIndex <= this.previousIndex ? -10 : 50;
            this.previousIndex = localIndex;

            switch (localIndex)
            {
                case 1:
                    this.regionManager.RequestNavigate(RegionNames.InformationRegion, typeof(Views.InformationAbout).FullName);
                    break;
                default:
                    break;
            }
        }
    }
}
