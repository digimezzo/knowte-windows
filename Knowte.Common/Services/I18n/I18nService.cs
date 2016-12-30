using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Xml.Linq;

namespace Knowte.Common.Services.I18n
{
    public class I18nService : II18nService
    {
        #region Private
        private string builtinLanguagesDirectory = System.IO.Path.Combine(ProcessExecutable.ExecutionFolder(), ApplicationPaths.BuiltinLanguagesSubDirectory);
        private string customLanguagesDirectory = System.IO.Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CustomLanguagesSubDirectory);
        private List<Language> languages;
        private Language defaultLanguage;
        private FileSystemWatcher languageWatcher;
        private Timer languageTimer = new Timer();
        private double languageTimeoutSeconds = 0.2;
        #endregion

        #region Construction
        public I18nService()
        {

            // Initialize the CustomLanguages directory
            // ----------------------------------------
            if (!Directory.Exists(this.customLanguagesDirectory))
            {
                Directory.CreateDirectory(System.IO.Path.Combine(this.customLanguagesDirectory));
            }

            this.LoadLanguages();


            // Configure the ColorSchemeTimer
            // ------------------------------
            this.languageTimer.Interval = TimeSpan.FromSeconds(this.languageTimeoutSeconds).TotalMilliseconds;
            this.languageTimer.Elapsed += new ElapsedEventHandler(LanguageTimerElapsed);


            // Start the LanguageWatcher
            // -------------------------
            this.languageWatcher = new FileSystemWatcher(this.customLanguagesDirectory);
            this.languageWatcher.EnableRaisingEvents = true;

            this.languageWatcher.Changed += new FileSystemEventHandler(WatcherChangedHandler);
            this.languageWatcher.Deleted += new FileSystemEventHandler(WatcherChangedHandler);
            this.languageWatcher.Created += new FileSystemEventHandler(WatcherChangedHandler);
            this.languageWatcher.Renamed += new RenamedEventHandler(WatcherRenamedHandler);
        }
        #endregion

        #region II18nService
        public async void ApplyLanguageAsync(string code)
        {
            await Task.Run(() =>
            {
                Language selectedLanguage = this.languages.Where(l => l.Code.ToUpper().Equals(code.ToUpper())).Select(l => l).FirstOrDefault();

                if (selectedLanguage == null)
                {
                    selectedLanguage = this.GetDefaultLanguage();
                }

                foreach (KeyValuePair<string, string> text in this.defaultLanguage.Texts)
                {
                    Application.Current.Resources["Language_" + text.Key] = this.GetTextValue(selectedLanguage, text.Key);
                }
            });

            if (LanguageChanged != null)
            {
                LanguageChanged(this, new EventArgs());
            }
        }

        public List<Language> GetLanguages()
        {
            return this.languages;
        }

        public Language GetLanguage(string code)
        {

            return this.languages.Where(l => l.Code.ToUpper().Equals(code.ToUpper())).Select(l => l).FirstOrDefault();
        }

        public Language GetDefaultLanguage()
        {

            return this.defaultLanguage;
        }
        #endregion

        #region Private
        private Language CreateLanguage(string languageFile)
        {
            XDocument xdoc = XDocument.Load(languageFile);

            Language returnLanguage = new Language();

            var languageInfo = (from t in xdoc.Elements("Language")
                                select t).FirstOrDefault();

            if (languageInfo != null)
            {
                returnLanguage.Code = languageInfo.Attribute("Code").Value;
                returnLanguage.Name = languageInfo.Attribute("Name").Value;
                returnLanguage.Author = languageInfo.Attribute("Author").Value;

                var textElements = (from t in xdoc.Element("Language").Elements("Text")
                                    select t).ToList();

                Dictionary<string, string> texts = new Dictionary<string, string>();


                foreach (XElement element in textElements)
                {
                    if (!texts.ContainsKey(element.Attribute("Key").Value))
                    {
                        texts.Add(element.Attribute("Key").Value, element.Value);
                    }
                }

                returnLanguage.Texts = texts;
            }

            return returnLanguage;
        }

        private void LoadLanguages()
        {
            if (this.languages == null)
            {
                this.languages = new List<Language>();
            }
            else
            {
                this.languages.Clear();
            }

            string[] builtinLanguageFiles = System.IO.Directory.GetFiles(this.builtinLanguagesDirectory, "*.xml");
            string[] customLanguageFiles = System.IO.Directory.GetFiles(this.customLanguagesDirectory, "*.xml");

            // First, get the custom languages

            foreach (string customLanguageFile in customLanguageFiles)
            {
                // Makes sure that unparsable language files don't crash the application
                try
                {
                    this.languages.Add(CreateLanguage(customLanguageFile));

                }
                catch (Exception)
                {
                }
            }

            // Then get the builtin languages

            foreach (string builtinLanguageFile in builtinLanguageFiles)
            {
                Language builtinlanguage = CreateLanguage(builtinLanguageFile);


                if (builtinlanguage != null)
                {
                    // Set the default language to the built-in English language
                    if (builtinlanguage.Code.ToUpper().Equals(Defaults.DefaultLanguageCode))
                    {
                        this.defaultLanguage = builtinlanguage;
                    }

                    // This makes sure custom languages have a higher priority than builtin languages.
                    //  This allows the user to customize.
                    if (!this.languages.Contains(builtinlanguage))
                    {
                        this.languages.Add(builtinlanguage);
                    }
                }
            }
        }


        private string GetTextValue(Language language, string key)
        {
            if (language.Texts.ContainsKey(key) && !string.IsNullOrEmpty(language.Texts[key]))
            {
                // If the key can be found in the selected language, return that
                return language.Texts[key];
            }
            else
            {
                // Otherwise, return the key from the default language
                return this.defaultLanguage.Texts[key];
            }
        }
        #endregion

        #region Events
        public event LanguagesChangedEventHandler LanguagesChanged;
        public event LanguageChangedEventHandler LanguageChanged;
        #endregion

        #region Event Handlers
        private void LanguageTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.languageTimer.Stop();

            Application.Current.Dispatcher.Invoke(() =>
            {
                this.LoadLanguages();

                if (LanguagesChanged != null)
                {
                    LanguagesChanged(this, null);
                }
            });
        }

        private void WatcherRenamedHandler(object sender, RenamedEventArgs e)
        {
            // Using a Timer here prevents that consecutive WatcherRenamed events trigger multiple CustomLanguagesChanged events
            this.languageTimer.Stop();
            this.languageTimer.Start();
        }

        private void WatcherChangedHandler(object sender, FileSystemEventArgs e)
        {
            // Using a Timer here prevents that consecutive WatcherChanged events trigger multiple CustomLanguagesChanged events
            this.languageTimer.Stop();
            this.languageTimer.Start();
        }
        #endregion
    }
}
