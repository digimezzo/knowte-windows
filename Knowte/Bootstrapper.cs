using Digimezzo.Utilities.Log;
using Knowte.Common.Base;
using Knowte.Common.Extensions;
using Knowte.Common.IO;
using Knowte.Common.Presentation.Views;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.Backup;
using Knowte.Common.Services.Command;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.I18n;
using Knowte.Common.Services.Notes;
using Knowte.Common.Services.Search;
using Knowte.Common.Services.WindowsIntegration;
using Knowte.ViewModels;
using Knowte.Views;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Unity;
using System;
using System.ServiceModel;
using System.Windows;

namespace Knowte
{
    public class Bootstrapper : UnityBootstrapper
    {
        protected override void ConfigureModuleCatalog()
        {
            base.ConfigureModuleCatalog();
            ModuleCatalog moduleCatalog = (ModuleCatalog)this.ModuleCatalog;

            moduleCatalog.AddModule(typeof(MainModule.MainModule));
            moduleCatalog.AddModule(typeof(NotesModule.NotesModule));
            moduleCatalog.AddModule(typeof(SettingsModule.SettingsModule));
            moduleCatalog.AddModule(typeof(InformationModule.InformationModule));
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            this.RegisterServices();
            this.RegisterViews();
            this.RegisterViewModels();

            ViewModelLocationProvider.SetDefaultViewModelFactory(type => { return Container.Resolve(type); });
        }

        protected void RegisterServices()
        {
            Container.RegisterSingletonType<II18nService, I18nService>();
            Container.RegisterSingletonType<IAppearanceService, AppearanceService>();
            Container.RegisterSingletonType<ISearchService, SearchService>();
            Container.RegisterSingletonType<IJumpListService, JumpListService>();
            Container.RegisterSingletonType<IDialogService, DialogService>();
            Container.RegisterSingletonType<IBackupService, BackupService>();
            Container.RegisterType<INotebookService, NotebookService>();
            Container.RegisterType<INoteService, NoteService>();
            Container.RegisterType<ICommandService, CommandService>();
        }

        protected void RegisterViews()
        {
            Container.RegisterType<object, Shell>(typeof(Shell).FullName);
            Container.RegisterType<object, Empty>(typeof(Empty).FullName);
        }

        protected void RegisterViewModels()
        {
            Container.RegisterType<object, ShellViewModel>(typeof(ShellViewModel).FullName);
        }

        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Shell>();
        }

        protected override void InitializeShell()
        {
            base.InitializeShell();

            this.InitializeWCFServices();
            Application.Current.MainWindow = (Window)this.Shell;
            Application.Current.MainWindow.Show();
        }

        protected void InitializeWCFServices()
        {
            // CommandService
            // --------------
            ServiceHost commandServicehost = new ServiceHost(typeof(CommandService), new Uri[] { new Uri(string.Format("net.pipe://localhost/{0}/CommandService", ProductInformation.ApplicationDisplayName)) });
            commandServicehost.AddServiceEndpoint(typeof(ICommandService), new StrongNetNamedPipeBinding(), "CommandServiceEndpoint");

            try
            {
                commandServicehost.Open();
                LogClient.Info("CommandService was started successfully");
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not start CommandService. Exception: {0}", ex.Message);
            }
        }
    }
}