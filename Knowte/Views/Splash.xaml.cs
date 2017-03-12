using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Packaging;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Database;
using Knowte.Common.IO;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Knowte.Views
{
    public partial class Splash : Window
    {
        #region Variables
        private int uiWaitMilliSeconds = 300;
        private string errorMessage;
        private Package package;
        #endregion

        #region Properties
        public Package Package
        {
            get
            {
                return this.package;
            }
        }

        public bool IsPreview
        {
            get
            {
#if DEBUG
                return true;
#else
		        return false;
#endif
            }
        }
        public bool ShowErrorPanel
        {
            get { return Convert.ToBoolean(GetValue(ShowErrorPanelProperty)); }

            set { SetValue(ShowErrorPanelProperty, value); }
        }

        public bool ShowProgressRing
        {
            get { return Convert.ToBoolean(GetValue(ShowProgressRingProperty)); }

            set { SetValue(ShowProgressRingProperty, value); }
        }
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty ShowErrorPanelProperty = DependencyProperty.Register("ShowErrorPanel", typeof(bool), typeof(Splash), new PropertyMetadata(null));
        public static readonly DependencyProperty ShowProgressRingProperty = DependencyProperty.Register("ShowProgressRing", typeof(bool), typeof(Splash), new PropertyMetadata(null));
        #endregion

        public Splash()
        {
            Configuration config;
#if DEBUG
            config = Configuration.Debug;
#else
		    config = Configuration.Release;
#endif

            this.package = new Package(ProcessExecutable.Name(), ProcessExecutable.AssemblyVersion(), config);

            InitializeComponent();
        }

        #region Private
        private void ShowError(string message)
        {
            this.ErrorMessage.Text = message;
            this.ShowErrorPanel = true;
        }

        private void ShowErrorDetails()
        {
            DateTime currentTime = DateTime.Now;
            string currentTimeString = currentTime.Year.ToString() + currentTime.Month.ToString() + currentTime.Day.ToString() + currentTime.Hour.ToString() + currentTime.Minute.ToString() + currentTime.Second.ToString() + currentTime.Millisecond.ToString();

            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), ProcessExecutable.Name() + "_" + currentTimeString + ".txt");
            System.IO.File.WriteAllText(path, this.errorMessage);
            System.Diagnostics.Process.Start(path);
        }

        private async void InitializeAsync()
        {
            bool continueInitializing = true;

            // Give the UI some time to show the progress ring
            await Task.Delay(this.uiWaitMilliSeconds);

            if (continueInitializing)
            {
                // Initialize the settings
                continueInitializing = await this.InitializeSettingsAsync();
            }

            if (continueInitializing)
            {
                // Initialize the settings
                continueInitializing = await this.InitializeNoteStorageDirectoryAsync();
            }

            if (continueInitializing)
            {
                // Initialize the database
                continueInitializing = await this.InitializeDatabaseAsync();
            }

            if (continueInitializing)
            {
                // If initializing was successful, start the application.
                if (this.ShowProgressRing)
                {
                    this.ShowProgressRing = false;

                    // Give the UI some time to hide the progress ring
                    await Task.Delay(this.uiWaitMilliSeconds);
                }

                var bootstrapper = new Bootstrapper();
                bootstrapper.Run();
                this.Close();
            }
            else
            {
                this.ShowError("I was not able to start. Please click 'Show details' for more information.");
            }
        }

        private async Task<bool> InitializeSettingsAsync()
        {
            bool isSuccess = false;

            try
            {
                // Checks if an upgrade of the settings is needed
                if (SettingsClient.IsUpgradeNeeded())
                {
                    this.ShowProgressRing = true;
                    LogClient.Info("Upgrading settings");
                    await Task.Run(() => SettingsClient.Upgrade());
                }

                isSuccess = true;
            }
            catch (Exception ex)
            {
                LogClient.Error("There was a problem initializing the settings. Exception: {0}", ex.Message);
                this.errorMessage = ex.Message;
                isSuccess = false;
            }

            return isSuccess;
        }

        private async Task<bool> InitializeNoteStorageDirectoryAsync()
        {
            bool isSuccess = true;

            // Make sure the default note storage location exists
            if (!System.IO.Directory.Exists(ApplicationPaths.DefaultNoteStorageLocation))
            {
                System.IO.Directory.CreateDirectory(ApplicationPaths.DefaultNoteStorageLocation);    
            }

            // Further verification is only needed when not using the default storage location
            if (ApplicationPaths.IsUsingDefaultStorageLocation) return true;

            try
            {
                await Task.Run(() =>
                {
                    var migrator = new DbMigrator();

                    if (!System.IO.Directory.Exists(ApplicationPaths.CurrentNoteStorageLocation))
                    {
                        LogClient.Warning("Note storage location '{0}' could not be found.", ApplicationPaths.CurrentNoteStorageLocation);
                        isSuccess = false;
                    }

                    if (!migrator.DatabaseExists())
                    {
                        LogClient.Warning("Database file '{0}' could not be found.", migrator.DatabaseFile);
                        isSuccess = false;
                    }

                    if (!isSuccess)
                    {
                        // Restore the default storage location
                        SettingsClient.Set<string>("General", "NoteStorageLocation", "");
                        LogClient.Warning("Default note storage location was restored.");

                        // Allow the application to start up after the default storage location was restored
                        isSuccess = true;
                    }
                });
            }
            catch (Exception ex)
            {
                LogClient.Error("There was a problem initializing the note storage location. Exception: {0}", ex.Message);
                this.errorMessage = ex.Message;
                isSuccess = false;
            }

            return isSuccess;
        }

        private async Task<bool> InitializeDatabaseAsync()
        {
            bool isSuccess = true;

            try
            {
                var migrator = new DbMigrator();

                if (!migrator.DatabaseExists())
                {
                    // Create the database if it doesn't exist
                    this.ShowProgressRing = true;
                    LogClient.Info("Creating database");
                    await Task.Run(() => migrator.InitializeNewDatabase());
                }
                else
                {
                    // Upgrade the database if it is not the latest version
                    if (migrator.DatabaseNeedsUpgrade())
                    {
                        this.ShowProgressRing = true;
                        LogClient.Info("Upgrading database");
                        await Task.Run(() => migrator.UpgradeDatabase());
                    }
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("There was a problem initializing the database. Exception: {0}", ex.Message);
                this.errorMessage = ex.Message;
                isSuccess = false;
            }

            return isSuccess;
        }
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.InitializeAsync();
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void BtnShowDetails_Click(object sender, RoutedEventArgs e)
        {
            this.ShowErrorDetails();
        }
    }
}
