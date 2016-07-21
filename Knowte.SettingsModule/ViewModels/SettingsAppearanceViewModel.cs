using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.I18n;
using Knowte.Core.IO;
using Knowte.Core.Settings;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Knowte.SettingsModule.ViewModels
{
    public class SettingsAppearanceViewModel : BindableBase
    {
        #region Variables
        private IAppearanceService appearanceService;

        private II18nService i18nService;
        private ObservableCollection<ColorScheme> colorSchemes = new ObservableCollection<ColorScheme>();
        private ColorScheme selectedColorScheme;
        private ObservableCollection<Language> languages;

        private Language selectedLanguage;
        private bool checkBoxWindowsColorChecked;

        private bool checkBoxSortChecked;
        private IEventAggregator eventAggregator;
        #endregion

        #region Properties
        public string ColorSchemesDirectory { get; set; }

        public ObservableCollection<ColorScheme> ColorSchemes
        {
            get { return this.colorSchemes; }
            set { SetProperty<ObservableCollection<ColorScheme>>(ref this.colorSchemes, value); }
        }

        public ColorScheme SelectedColorScheme
        {
            get { return this.selectedColorScheme; }

            set
            {
                if (!this.CheckBoxWindowsColorChecked)
                {
                    // value can be Nothing when a ColorScheme has is removed from the ColorSchemes directory

                    if (value != null)
                    {
                        XmlSettingsClient.Instance.Set<string>("Appearance", "ColorScheme", value.Name);
                        this.appearanceService.ApplyColorScheme(this.CheckBoxWindowsColorChecked, value.Name);
                    }

                    SetProperty<ColorScheme>(ref this.selectedColorScheme, value);
                }
            }
        }

        public ObservableCollection<Language> Languages
        {
            get { return this.languages; }
            set { SetProperty<ObservableCollection<Language>>(ref this.languages, value); }
        }

        public Language SelectedLanguage
        {
            get { return this.selectedLanguage; }
            set
            {
                SetProperty<Language>(ref this.selectedLanguage, value);

                XmlSettingsClient.Instance.Set<string>("Appearance", "Language", value.Code);
                Application.Current.Dispatcher.Invoke(() => this.i18nService.ApplyLanguageAsync(value.Code));
            }
        }

        public bool CheckBoxWindowsColorChecked
        {
            get { return this.checkBoxWindowsColorChecked; }
            set
            {
                SetProperty<bool>(ref this.checkBoxWindowsColorChecked, value);

                XmlSettingsClient.Instance.Set<bool>("Appearance", "FollowWindowsColor", value);

                if (this.SelectedColorScheme != null)
                {
                    this.appearanceService.ApplyColorScheme(value, this.SelectedColorScheme.Name);
                }
            }
        }

        public bool CheckBoxSortChecked
        {
            get { return this.checkBoxSortChecked; }
            set
            {
                SetProperty<bool>(ref this.checkBoxSortChecked, value);

                XmlSettingsClient.Instance.Set<bool>("Appearance", "SortByModificationDate", value);
                this.eventAggregator.GetEvent<RefreshNotesEvent>().Publish("");
            }
        }
        #endregion

        #region Construction
        public SettingsAppearanceViewModel(IAppearanceService appearanceService, II18nService i18nService, IEventAggregator eventAggregator)
        {
            this.appearanceService = appearanceService;
            this.i18nService = i18nService;
            this.eventAggregator = eventAggregator;

            // ColorSchemes
            this.GetColorSchemes();

            // Languages
            this.GetLanguagesAsync();

            // CheckBoxStates
            this.LoadCheckBoxStates();

            // Event handling
            this.appearanceService.ColorSchemesChanged += ColorSchemesChangedHandler;
            this.i18nService.LanguagesChanged += (sender, e) => this.GetLanguagesAsync();

            this.ColorSchemesDirectory = System.IO.Path.Combine(XmlSettingsClient.Instance.ApplicationFolder, ApplicationPaths.ColorSchemesSubDirectory);
        }
        #endregion

        #region Private
        private void LoadCheckBoxStates()
        {
            this.CheckBoxWindowsColorChecked = XmlSettingsClient.Instance.Get<bool>("Appearance", "FollowWindowsColor");
            this.CheckBoxSortChecked = XmlSettingsClient.Instance.Get<bool>("Appearance", "SortByModificationDate");
        }

        private void GetColorSchemes()
        {
            this.ColorSchemes.Clear();

            foreach (ColorScheme cs in this.appearanceService.GetColorSchemes())
            {
                this.ColorSchemes.Add(cs);
            }

            string savedColorSchemeName = XmlSettingsClient.Instance.Get<string>("Appearance", "ColorScheme");

            if (!string.IsNullOrEmpty(savedColorSchemeName))
            {
                this.SelectedColorScheme = this.appearanceService.GetColorScheme(savedColorSchemeName);
            }
            else
            {
                this.SelectedColorScheme = this.appearanceService.GetColorSchemes()[0];
            }
        }

        private async void GetLanguagesAsync()
        {
            List<Language> languagesList = this.i18nService.GetLanguages();

            ObservableCollection<Language> localLanguages = new ObservableCollection<Language>();

            await Task.Run(() =>
            {
                foreach (Language lang in languagesList)
                {
                    localLanguages.Add(lang);
                }
            });

            this.Languages = localLanguages;

            Language tempLanguage = null;

            await Task.Run(() =>
            {
                string savedLanguageCode = XmlSettingsClient.Instance.Get<string>("Appearance", "Language");

                if (!string.IsNullOrEmpty(savedLanguageCode))
                {
                    tempLanguage = this.i18nService.GetLanguage(savedLanguageCode);
                }

                // If, for some reason, SelectedLanguage is Nothing (e.g. when the user 
                // deleted a previously existing language file), select the default language.
                if (tempLanguage == null)
                {
                    tempLanguage = this.i18nService.GetDefaultLanguage();
                }
            });

            // Only set SelectedLanguage when we are sure that it is not Nothing. Otherwise this could trigger strange 
            // behaviour in the setter of the SelectedLanguage Property (because the "value" would be Nothing)
            this.SelectedLanguage = tempLanguage;
        }
        #endregion

        #region Event Handlers
        private void ColorSchemesChangedHandler(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => { this.GetColorSchemes(); });
        }
        #endregion
    }
}