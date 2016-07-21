using System;
using System.Collections.Generic;
using System.Windows;

namespace Knowte.Common.Services.Appearance
{
    public delegate void ColorSchemesChangedEventHandler(object sender, EventArgs e);
    public delegate void AppearanceChangedEventHandler(object sender, EventArgs e);

    public interface IAppearanceService
    {
        string ColorSchemesSubDirectory { get; set; }
        List<ColorScheme> GetColorSchemes();
        List<string> GetThemes();
        ColorScheme GetColorScheme(string name);
        void ApplyTheme(string name);
        void ApplyColorScheme(bool followWindowsColor, string selectedColorScheme = "");
        void WatchWindowsColor(Window win);
        event AppearanceChangedEventHandler AppearanceChanged;
        event ColorSchemesChangedEventHandler ColorSchemesChanged;
    }
}