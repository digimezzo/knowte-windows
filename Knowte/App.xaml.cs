using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Database;
using Knowte.Common.IO;
using Knowte.Common.Services.Command;
using System;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Knowte
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Application-level events, such as Startup, Exit, and DispatcherUnhandledException
        // can be handled in this file.

        private Mutex instanceMutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check if the settings need to be upgraded
            try
            {
                // Checks if an upgrade of the settings is needed
                if (SettingsClient.IsUpgradeNeeded())
                {
                    LogClient.Info("Upgrading settings");
                    SettingsClient.Upgrade();
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("There was a problem initializing the settings. Exception: {0}", ex.Message);
                this.Shutdown();
            }

            // Create a jumplist and assign it to the current application
            JumpList jl = new JumpList();
            JumpList.SetJumpList(Application.Current, jl);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // Check that there is only one instance of NoteStudio running...
            bool isNewInstance = false;
            instanceMutex = new Mutex(true, string.Format("{0}-{1}", ProductInformation.ApplicationGuid, ProcessExecutable.AssemblyVersion().ToString()), out isNewInstance);

            // Get the commandline arguments
            string[] args = Environment.GetCommandLineArgs();

            try
            {
                if (args.Length > 1)
                {
                    switch (args[1])
                    {
                        case "/donate":
                            LogClient.Info("Detected 'Donate' command from jumplist.");
                            Actions.TryOpenLink(args[2]);
                            this.Shutdown();
                            return;
                        default:
                            break;
                            // Do nothing
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("A problem occured while processing Donate command. Exception: {0}", ex.Message);
            }

            if (!isNewInstance)
            {
                LogClient.Info("There is already another instance of {0} running.", ProductInformation.ApplicationDisplayName);

                instanceMutex = null;

                // Do fancy stuff here to send to the already running instance
                ICommandService commandServiceProxy = default(ICommandService);
                ChannelFactory<ICommandService> commandServiceFactory = new ChannelFactory<ICommandService>(new StrongNetNamedPipeBinding(), new EndpointAddress(string.Format("net.pipe://localhost/{0}/CommandService/CommandServiceEndpoint", ProductInformation.ApplicationDisplayName)));

                try
                {
                    commandServiceProxy = commandServiceFactory.CreateChannel();
                    LogClient.Info("Trying to show the running instance");

                    if (args.Length > 1)
                    {
                        switch (args[1])
                        {
                            case "/new":
                                LogClient.Info("Sending 'New note' command to running instance.");
                                commandServiceProxy.NewNote();
                                break;
                            case "/open":
                                LogClient.Info("Sending 'Open note: {0}' command to running instance.", args[2]);
                                commandServiceProxy.OpenNote(args[2]);
                                break;
                            default:
                                break;
                                // Do nothing
                        }
                    }
                    else
                    {
                        LogClient.Info("Forcing running instance to show main window.");
                        commandServiceProxy.Show();
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("A problem occured while trying to show the running instance. Exception: {0}", ex.Message);
                }

                this.Shutdown();
            }
            else
            {
                // This piece of code is only executed when there is 
                // no other instance of the application running.
                LogClient.Info("### STARTING {0}, version {1} ###", ProductInformation.ApplicationDisplayName, ProcessExecutable.AssemblyVersion().ToString());

                // Show SplashScreen
                SplashScreen splash = new SplashScreen(Assembly.LoadFrom(System.IO.Path.Combine(ProcessExecutable.ExecutionFolder(), Assembly.GetEntryAssembly().GetName().Name + ".exe")), "Splash.png");
                splash.Show(true);

                // Create or upgrade the database
                var creator = new DbCreator();

                if (!creator.DatabaseExists())
                {
                    creator.InitializeNewDatabase();
                }
                else if (creator.DatabaseNeedsUpgrade())
                {
                    creator.UpgradeDatabase();
                }

                // Bootstrapper
                Bootstrapper bootstrapper = new Bootstrapper();
                bootstrapper.Run();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            // Log the exception and stop the application
            this.ExecuteEmergencyStop(ex);
        }


        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Prevent default unhandled exception processing
            e.Handled = true;

            if (e.GetType().ToString().Equals("System.OutOfMemoryException"))
            {
                LogClient.Warning("Ignored System.OutOfMemoryException");
                return;
            }

            this.ExecuteEmergencyStop(e.Exception);
        }


        private void ExecuteEmergencyStop(Exception iException)
        {
            LogClient.Error(iException.Message);

            // Ignore System.OutOfMemoryException. This sometimes happen when pasting invalid data from the clipboard.
            if (iException.GetType().ToString().Equals("System.OutOfMemoryException"))
            {
                LogClient.Warning("Ignored System.OutOfMemoryException");
                return;
            }

            this.Shutdown();
        }
    }
}
