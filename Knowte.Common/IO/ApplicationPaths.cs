using Digimezzo.Utilities.Settings;

namespace Knowte.Common.IO
{
    public static class ApplicationPaths
    {
        public static string NotesSubDirectory = "Notes";
        public static string BackupSubDirectory = "Backup";
        public static string ColorSchemesSubDirectory = "ColorSchemes";
        public static string BuiltinLanguagesSubDirectory = "Languages";
        public static string CustomLanguagesSubDirectory = "Languages";

        public static string DefaultNoteStorageLocation
        {
            get
            {
                return System.IO.Path.Combine(SettingsClient.ApplicationFolder(), NotesSubDirectory);
            }
        }
        public static string CurrentNoteStorageLocation
        {
            get
            {
                if (IsUsingDefaultStorageLocation)
                {
                    return DefaultNoteStorageLocation;
                }
                else
                {
                    return SettingsClient.Get<string>("General", "NoteStorageLocation");
                }
            }
        }

        public static bool IsUsingDefaultStorageLocation
        {
            get
            {
                return string.IsNullOrWhiteSpace(SettingsClient.Get<string>("General", "NoteStorageLocation"));
            }
        }
    }
}
