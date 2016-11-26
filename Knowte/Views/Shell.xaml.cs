using Knowte.Common.Controls;
using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.I18n;
using Knowte.Common.Services.Notes;
using Knowte.Common.Services.WindowsIntegration;
using Knowte.Core.Base;
using Knowte.Core.Extensions;
using Knowte.Core.Logging;
using Knowte.Core.Settings;
using Knowte.Core.Utils;
using Microsoft.Practices.Unity;
using Prism.Events;
using Prism.Regions;
using System;
using System.Windows;
using System.Windows.Input;

namespace Knowte.Views
{
    public partial class Shell : KnowteWindow
    {
        #region Variables
        private IUnityContainer container;
        private readonly IRegionManager regionManager;
        private IEventAggregator eventAggregator;
        private IAppearanceService appearanceService;
        private II18nService i18nService;
        private IJumpListService jumplistService;
        private INoteService noteService;
        #endregion

        #region Properties
        public new object DataContext
        {
            get { return base.DataContext; }
            set { base.DataContext = value; }
        }
        #endregion

        #region Construction
        public Shell(IUnityContainer container, IRegionManager regionManager, IAppearanceService appearanceService, II18nService i18nService, IJumpListService jumpListService, IEventAggregator eventAggregator, INoteService noteService)
        {
            SourceInitialized += Shell_SourceInitialized;
            // This call is required by the designer.
            InitializeComponent();

            // Dependency injection
            this.container = container;
            this.regionManager = regionManager;
            this.appearanceService = appearanceService;
            this.i18nService = i18nService;
            this.jumplistService = jumpListService;
            this.eventAggregator = eventAggregator;
            this.noteService = noteService;

            // PubSubEvents
            this.eventAggregator.GetEvent<SettingShowWindowBorderChanged>().Subscribe(showWindowBorder => this.SetWindowBorder(showWindowBorder));

            // Theming
            this.appearanceService.ApplyTheme(XmlSettingsClient.Instance.Get<string>("Appearance", "Theme"));
            this.appearanceService.ApplyColorScheme(XmlSettingsClient.Instance.Get<bool>("Appearance", "FollowWindowsColor"), XmlSettingsClient.Instance.Get<string>("Appearance", "ColorScheme"));

            // I18n
            this.i18nService.ApplyLanguageAsync(XmlSettingsClient.Instance.Get<string>("Appearance", "Language"));

            // Events
            this.eventAggregator.GetEvent<ShowMainWindowEvent>().Subscribe((x) => this.ActivateNow());

            this.SetGeometry(XmlSettingsClient.Instance.Get<int>("General", "Top"), XmlSettingsClient.Instance.Get<int>("General", "Left"), XmlSettingsClient.Instance.Get<int>("General", "Width") > 50 ? XmlSettingsClient.Instance.Get<int>("General", "Width") : Defaults.DefaultMainWindowWidth, XmlSettingsClient.Instance.Get<int>("General", "Height") > 50 ? XmlSettingsClient.Instance.Get<int>("General", "Height") : Defaults.DefaultMainWindowHeight, Defaults.DefaultMainWindowLeft, Defaults.DefaultMainWindowTop);

            // Main window state
            this.WindowState = XmlSettingsClient.Instance.Get<bool>("General", "IsMaximized") ? WindowState.Maximized : WindowState.Normal;
        }
        #endregion

        #region Private
        private void ProcessCommandLineArgs()
        {
            // Get the commandline arguments
            string[] args = Environment.GetCommandLineArgs();


            if (args.Length > 1)
            {
                switch (args[1])
                {
                    case "/new":
                        this.WindowState = WindowState.Minimized;
                        this.jumplistService.NewNoteFromJumplist = true;

                        break;
                    case "/open":
                        this.WindowState = WindowState.Minimized;
                        this.jumplistService.OpenNoteFromJumplist = true;
                        this.jumplistService.OpenNoteFromJumplistTitle = args[2];
                        break;
                    default:
                        break;
                        // Do nothing
                }
            }
        }
        #endregion

        #region Event handlers
        private void Shell_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == null & char.IsLetterOrDigit((char)e.Key) & !(e.Key == Key.F1 | e.Key == Key.F2 | e.Key == Key.F3 | e.Key == Key.F4 | e.Key == Key.F5 | e.Key == Key.F6 | e.Key == Key.F7 | e.Key == Key.F8 | e.Key == Key.F9 | e.Key == Key.F10 | e.Key == Key.F11 | e.Key == Key.F12))
            {
                this.eventAggregator.GetEvent<SetMainSearchBoxFocusEvent>().Publish(string.Empty);
            }
        }

        private void Shell_SourceInitialized(object sender, EventArgs e)
        {
            this.appearanceService.WatchWindowsColor(this);
        }

        private void Shell_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogClient.Instance.Logger.Info("### STOPPING {0}, version {1} ###", ProductInformation.ApplicationDisplayName, ProductInformation.AssemblyVersion.ToString());

            // Prevent saving the size when the window is minimized.
            // When minimized, the actual size is not detected correctly,
            // which causes a too small size to be saved.

            if (!(this.WindowState == WindowState.Minimized))
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    XmlSettingsClient.Instance.Set<bool>("General", "IsMaximized", true);
                }
                else
                {
                    XmlSettingsClient.Instance.Set<bool>("General", "IsMaximized", false);

                    // TODO: make tis better. Workaround for bug "MainWindow opens with size 0 px"
                    if (this.ActualWidth > 50 & this.ActualHeight > 50)
                    {
                        XmlSettingsClient.Instance.Set<int>("General", "Width", (int)this.ActualWidth);
                        XmlSettingsClient.Instance.Set<int>("General", "Height", (int)this.ActualHeight);
                    }
                    else
                    {
                        XmlSettingsClient.Instance.Set<int>("General", "Width", Defaults.DefaultMainWindowWidth);
                        XmlSettingsClient.Instance.Set<int>("General", "Height", Defaults.DefaultMainWindowHeight);
                    }

                    XmlSettingsClient.Instance.Set<int>("General", "Top", (int)this.Top);
                    XmlSettingsClient.Instance.Set<int>("General", "Left", (int)this.Left);
                }

                // Save the settings immediately
                XmlSettingsClient.Instance.Write();
            }

            foreach (Window win in Application.Current.Windows)
            {

                if (!win.Equals(Application.Current.MainWindow))
                {
                    win.Close();
                }
            }
        }

        private void Shell_Loaded(object sender, RoutedEventArgs e)
        {
            // Enable blur
            if (EnvironmentUtils.IsWindows10())
            {
                this.HeaderBackground.Opacity = Constants.OpacityWhenBlurred;
                WindowUtils.EnableBlur(this);
            }else
            {
                this.HeaderBackground.Opacity = 1.0;
            }

            this.ProcessCommandLineArgs();
            this.jumplistService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(XmlSettingsClient.Instance.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());

        }

        private void Shell_MouseDown(object sender, MouseButtonEventArgs e)
        {
            foreach (Window win in Application.Current.Windows)
            {
                win.Topmost = false;
            }
        }
        #endregion
    }
}