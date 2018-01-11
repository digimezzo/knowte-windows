using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Presentation.Views;
using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.I18n;
using Knowte.Common.Utils;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;

namespace Knowte.ViewModels
{
    public class ShellViewModel : BindableBase
    {
        private IAppearanceService appearanceService;
        private II18nService i18nService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private IRegionManager regionManager;
        private bool isDimmed;
        private int subMenuSlideInFrom;
        private int contentSlideInFrom;
        private int searchSlideInFrom;
        private int previousIndex;
      
        public DelegateCommand<string> OpenLinkCommand;
        public DelegateCommand<string> OpenPathCommand;
        public DelegateCommand<string> NavigateBetweenMainCommand { get; set; }
    
        public bool IsDimmed
        {
            get { return this.isDimmed; }
            set { SetProperty<bool>(ref this.isDimmed, value); }
        }

        public int SubMenuSlideInFrom
        {
            get { return this.subMenuSlideInFrom; }
            set { SetProperty<int>(ref this.subMenuSlideInFrom, value); }
        }

        public int ContentSlideInFrom
        {
            get { return this.contentSlideInFrom; }
            set { SetProperty<int>(ref this.contentSlideInFrom, value); }
        }

        public int SearchSlideInFrom
        {
            get { return this.searchSlideInFrom; }
            set { SetProperty<int>(ref this.searchSlideInFrom, value); }
        }
  
        public ShellViewModel(IAppearanceService appearanceService, II18nService i18nService, IDialogService dialogService, IEventAggregator eventAggregator, IRegionManager regionManager)
        {
            // Dependency injection
            this.regionManager = regionManager;
            this.appearanceService = appearanceService;
            this.i18nService = i18nService;
            this.dialogService = dialogService;
            this.eventAggregator = eventAggregator;

            // Theming
            this.appearanceService.ApplyColorScheme(SettingsClient.Get<bool>("Appearance", "FollowWindowsColor"), SettingsClient.Get<string>("Appearance", "ColorScheme"));

            // I18n
            this.i18nService.ApplyLanguageAsync(SettingsClient.Get<string>("Appearance", "Language"));

            // Commands
            this.OpenPathCommand = new DelegateCommand<string>((string path) =>
            {
                try
                {
                    Actions.TryOpenPath(path);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not open the path {0} in Explorer. Exception: {1}", path, ex.Message);
                }
            });

            ApplicationCommands.OpenPathCommand.RegisterCommand(this.OpenPathCommand);

            this.OpenLinkCommand = new DelegateCommand<string>((string link) =>
            {
                try
                {
                    Actions.TryOpenLink(link);
                }
                catch (Exception ex)
                {
                    LogClient.Error("Could not open the link {0}. Exception: {1}", link, ex.Message);
                }
            });

            ApplicationCommands.OpenLinkCommand.RegisterCommand(this.OpenLinkCommand);

            this.NavigateBetweenMainCommand = new DelegateCommand<string>((index) => this.NavigateBetweenMain(index));
            ApplicationCommands.NavigateBetweenMainCommand.RegisterCommand(this.NavigateBetweenMainCommand);

            // Events
            this.dialogService.DialogVisibleChanged += isDialogVisible => this.IsDimmed = isDialogVisible;

            this.SubMenuSlideInFrom = 40;
            this.ContentSlideInFrom = 30;
            this.SearchSlideInFrom = 20;
        }
   
        private void NavigateBetweenMain(string index)
        {
            if (string.IsNullOrWhiteSpace(index)) return;

            int localIndex = 0;

            int.TryParse(index, out localIndex);

            if (localIndex == 0) return;

            this.SubMenuSlideInFrom = localIndex <= this.previousIndex ? 0 : 40;
            this.ContentSlideInFrom = localIndex <= this.previousIndex ? -30 : 30;
            this.SearchSlideInFrom = localIndex <= this.previousIndex ? -20 : 20;

            this.previousIndex = localIndex;

            switch (localIndex)
            {
                case 1:
                    this.regionManager.RequestNavigate(RegionNames.SearchRegion, typeof(MainModule.Views.Search).FullName);
                    this.regionManager.RequestNavigate(RegionNames.SubMenuRegion, typeof(NotesModule.Views.NotesSubMenu).FullName);
                    this.regionManager.RequestNavigate(RegionNames.ContentRegion, typeof(NotesModule.Views.Notes).FullName);
                    break;
                case 2:
                    this.regionManager.RequestNavigate(RegionNames.SearchRegion, typeof(Empty).FullName);
                    this.regionManager.RequestNavigate(RegionNames.SubMenuRegion, typeof(SettingsModule.Views.SettingsSubMenu).FullName);
                    this.regionManager.RequestNavigate(RegionNames.ContentRegion, typeof(SettingsModule.Views.Settings).FullName);
                    break;
                case 3:
                    this.regionManager.RequestNavigate(RegionNames.SearchRegion, typeof(Empty).FullName);
                    this.regionManager.RequestNavigate(RegionNames.SubMenuRegion, typeof(InformationModule.Views.InformationSubMenu).FullName);
                    this.regionManager.RequestNavigate(RegionNames.ContentRegion, typeof(InformationModule.Views.Information).FullName);
                    break;
                default:
                    break;
            }
        }
    }
}
