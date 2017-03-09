using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Base;
using Knowte.Common.IO;
using Knowte.Common.Services.Command;
using Knowte.Views;
using System;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Knowte
{
    public partial class App : Application
    {
        #region Variables
        private Mutex instanceMutex = null;
        #endregion

        #region Overrides
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check that there is only one instance of the application running
            bool isNewInstance = false;
            instanceMutex = new Mutex(true, string.Format("{0}-{1}", ProductInformation.ApplicationGuid, ProcessExecutable.AssemblyVersion().ToString()), out isNewInstance);

            // Process the commandline arguments
            this.ProcessCommandLineArguments(isNewInstance);

            if (isNewInstance)
            {
                instanceMutex.ReleaseMutex();
                this.ExecuteStartup();
            }
            else
            {
                LogClient.Warning("{0} is already running. Shutting down.", ProcessExecutable.Name());
                this.Shutdown();
            }
        }
        #endregion

        #region Private
        private void ExecuteStartup()
        {
            LogClient.Info("### STARTING {0}, version {1}, IsPortable = {2}, Windows version = {3} ({4}) ###", ProcessExecutable.Name(), ProcessExecutable.AssemblyVersion().ToString(), SettingsClient.BaseGet<bool>("Configuration", "IsPortable").ToString(), EnvironmentUtils.GetFriendlyWindowsVersion(), EnvironmentUtils.GetInternalWindowsVersion());

            // Handler for unhandled AppDomain exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);


            // Create a jumplist and assign it to the current application
            JumpList jl = new JumpList();
            JumpList.SetJumpList(Application.Current, jl);

            // Show the Splash Window
            Window splashWin = new Splash();
            splashWin.Show();
        }

        private void ProcessCommandLineArguments(bool isNewInstance)
        {
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
                LogClient.Error("A problem occurred while processing Donate command. Exception: {0}", ex.Message);
            }

            if (!isNewInstance)
            {
                LogClient.Info("There is already another instance of {0} running.", ProductInformation.ApplicationDisplayName);

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
                    LogClient.Error("A problem occurred while trying to show the running instance. Exception: {0}", ex.Message);
                }
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

            // Log the exception and stop the application
            this.ExecuteEmergencyStop(e.Exception);
        }

        private void ExecuteEmergencyStop(Exception ex)
        {
            LogClient.Error("Unhandled Exception. {0}", LogClient.GetAllExceptions(ex));

            // Close the application to prevent further problems
            LogClient.Info("### FORCED STOP of {0}, version {1} ###", ProcessExecutable.Name(), ProcessExecutable.AssemblyVersion().ToString());

            // Emergency save of the settings
            SettingsClient.Write();

            Application.Current.Shutdown();
        }
        #endregion
    }
}
