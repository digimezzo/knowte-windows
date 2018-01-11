using Digimezzo.Utilities.Settings;
using Knowte.Common.IO;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.I18n;
using Knowte.Common.Services.Note;
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
        private IAppearanceService appearanceService;
        private II18nService i18nService;
        private INoteService noteService;
        private ObservableCollection<ColorScheme> colorSchemes = new ObservableCollection<ColorScheme>();
        private ColorScheme selectedColorScheme;
        private ObservableCollection<Language> languages;

        private Language selectedLanguage;
        private bool checkBoxWindowsColorChecked;
        private bool checkBoxSortChecked;
        private IEventAggregator eventAggregator;
     
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
                        SettingsClient.Set<string>("Appearance", "ColorScheme", value.Name);
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

                SettingsClient.Set<string>("Appearance", "Language", value.Code);
                Application.Current.Dispatcher.Invoke(() => this.i18nService.ApplyLanguageAsync(value.Code));
            }
        }

        public bool CheckBoxWindowsColorChecked
        {
            get { return this.checkBoxWindowsColorChecked; }
            set
            {
                SetProperty<bool>(ref this.checkBoxWindowsColorChecked, value);

                SettingsClient.Set<bool>("Appearance", "FollowWindowsColor", value);

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

                SettingsClient.Set<bool>("Appearance", "SortByModificationDate", value);
                this.noteService.OnNotesChanged();
            }
        }
        
        public SettingsAppearanceViewModel(IAppearanceService appearanceService, II18nService i18nService,INoteService noteService, IEventAggregator eventAggregator)
        {
            this.appearanceService = appearanceService;
            this.i18nService = i18nService;
            this.noteService = noteService;
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

            this.ColorSchemesDirectory = System.IO.Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.ColorSchemesSubDirectory);
        }
       
        private void LoadCheckBoxStates()
        {
            this.CheckBoxWindowsColorChecked = SettingsClient.Get<bool>("Appearance", "FollowWindowsColor");
            this.CheckBoxSortChecked = SettingsClient.Get<bool>("Appearance", "SortByModificationDate");
        }

        private void GetColorSchemes()
        {
            this.ColorSchemes.Clear();

            foreach (ColorScheme cs in this.appearanceService.GetColorSchemes())
            {
                this.ColorSchemes.Add(cs);
            }

            string savedColorSchemeName = SettingsClient.Get<string>("Appearance", "ColorScheme");

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
                string savedLanguageCode = SettingsClient.Get<string>("Appearance", "Language");

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
       
        private void ColorSchemesChangedHandler(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => { this.GetColorSchemes(); });
        }
    }
}