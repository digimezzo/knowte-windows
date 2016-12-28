using System;

namespace Knowte.Common.Base
{
    public sealed class Defaults
    {
        public static string IconsLibrary = "Knowte.Icons.dll";
        public static string[] Languages = { "EN", "NL", "FR" };
        public static string DefaultLanguageCode  = "EN";
        public static string[] Themes = { "Light" };
        public static string NoteFont = "Calibri";
        public static string NoteFWFont = "Courier New";
        public static double DefaultWindowButtonHeight = 26;
        public static int DefaultMainWindowWidth = 950;
        public static int DefaultMainWindowHeight = 500;
        public static int DefaultMainWindowTop = 50;
        public static int DefaultMainWindowLeft = 50;
        public static int DefaultNoteFontSize = 15;
        public static int DefaultNoteWidth = 550;
        public static int DefaultNoteHeight = 450;
        public static int DefaultNoteTop = 50;
        public static int DefaultNoteLeft = 50;
        public static string PrintTitleColor = "#2E74B5";
        public static string PrintLinkColor = "#0563C1";
        public static string MailRegex = @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}\b"; // As found at http://www.regular-expressions.info/email.html
        public static string UrlRegex = @"^(file|http|https):/{2}[a-zA-Z./&\d_-]+"; // As found at http://www.dotnetfunda.com/codes/show/1519/regex-pattern-to-validate-url
        public static int ShowNotificationSeconds = 10;
        public static int AnimationDuration = 50;
        public static string ExportFileExtension = "knowte";
    }
}
