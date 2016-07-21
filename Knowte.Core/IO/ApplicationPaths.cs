using Knowte.Core.Base;

namespace Knowte.Core.IO
{
    public sealed class ApplicationPaths
    {
        public static string NotesSubDirectory = "Notes";
        public static string ColorSchemesSubDirectory = "ColorSchemes";
        public static string LogSubDirectory = "Log";
        public static string LogFile = ProductInformation.ApplicationDisplayName + ".log";
        public static string LogArchiveFile = ProductInformation.ApplicationDisplayName + ".{#}.log";
        public static string ExecutionFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string BuiltinLanguagesSubDirectory = "Languages";
        public static string CustomLanguagesSubDirectory = "Languages";
    }
}
