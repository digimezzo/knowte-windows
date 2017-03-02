using Digimezzo.Utilities.Settings;

namespace Knowte.Common.IO
{
    public sealed class ApplicationPaths
    {
        public static string NotesSubDirectory = "Notes";
        public static string BackupSubDirectory = "Backup";
        public static string ColorSchemesSubDirectory = "ColorSchemes";
        public static string BuiltinLanguagesSubDirectory = "Languages";
        public static string CustomLanguagesSubDirectory = "Languages";

        public static string NoteStorageLocation
        {
            get
            {
                string customStorageLocation = SettingsClient.Get<string>("General", "NoteStorageLocation");

                if (string.IsNullOrWhiteSpace(customStorageLocation)){
                    return SettingsClient.ApplicationFolder();
                }
                else
                {
                    return customStorageLocation;
                }
            }
        }
    }
}
