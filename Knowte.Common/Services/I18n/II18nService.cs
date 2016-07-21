using System;
using System.Collections.Generic;

namespace Knowte.Common.Services.I18n
{
    public delegate void LanguageChangedEventHandler(object sender, EventArgs e);
    public delegate void LanguagesChangedEventHandler(object sender, EventArgs e);

    public interface II18nService
    {
        List<Language> GetLanguages();
        Language GetLanguage(string code);
        Language GetDefaultLanguage();
        void ApplyLanguageAsync(string code);
        event LanguagesChangedEventHandler LanguagesChanged;
        event LanguageChangedEventHandler LanguageChanged;
    }
}