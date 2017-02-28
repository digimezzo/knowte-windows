using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Database;
using Knowte.Common.IO;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Knowte.Common.Services.Backup
{
    public class BackupService : IBackupService
    {
        #region Variables
        private SQLiteConnectionFactory factory;
        private string applicationFolder = SettingsClient.ApplicationFolder();
        #endregion

        #region Construction
        public BackupService()
        {
            this.factory = new SQLiteConnectionFactory();
        }
        #endregion

        #region IBackupService
        public async Task<bool> BackupAsync(string backupFile)
        {
            if (string.IsNullOrWhiteSpace(backupFile))
            {
                LogClient.Error("Could not perform backup: backupFile is empty.");
                return false;
            }

            bool isSuccess = true;

            try
            {
                await Task.Run(() =>
                {
                    string tempFile = backupFile + ".temp";

                    using (ZipArchive archive = ZipFile.Open(tempFile, ZipArchiveMode.Create))
                    {
                        // Add the "Notes" subfolder    
                        var di = new DirectoryInfo(Path.Combine(this.applicationFolder, ApplicationPaths.NotesSubDirectory));
                        FileInfo[] fi = di.GetFiles();

                        foreach (FileInfo f in fi)
                        {
                            archive.CreateEntryFromFile(f.FullName, f.Directory.Name + "/" + f.Name);
                        }

                        // Add database file
                        archive.CreateEntryFromFile(this.factory.DatabaseFile, Path.GetFileName(this.factory.DatabaseFile));
                    }

                    File.Move(tempFile, backupFile);
                });
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform backup. Exception: {0}", ex.Message);
            }

            return isSuccess;
        }

        public async Task<bool> RestoreAsync(string backupFile)
        {
            if (string.IsNullOrWhiteSpace(backupFile))
            {
                LogClient.Error("Could not perform backup: backupFile is empty.");
                return false;
            }

            bool isSuccess = true;

            string notesDirectoryPath = Path.Combine(this.applicationFolder, ApplicationPaths.NotesSubDirectory);

            try
            {
                Directory.Move(notesDirectoryPath, notesDirectoryPath + ".old"); // Move Notes directory to Notes.old
                File.Move(this.factory.DatabaseFile, this.factory.DatabaseFile + ".old"); // Move Knowte.db to Knowte.db.old.

                // Restore backup
                ZipFile.ExtractToDirectory(backupFile, this.applicationFolder);

                Directory.Delete(notesDirectoryPath + ".old", true); // Delete Notes.old
                File.Delete(this.factory.DatabaseFile + ".old"); // Delete Knowte.db.old
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform restore. Exception: {0}", ex.Message);

                try
                {
                    // If restore fails, restore from .old files
                    LogClient.Error("Trying to restore original files.");

                    if(File.Exists(this.factory.DatabaseFile)) File.Delete(this.factory.DatabaseFile); // Delete Knowte.db
                    if (Directory.Exists(notesDirectoryPath)) Directory.Delete(notesDirectoryPath, true); // Delete Notes

                    Directory.Move(notesDirectoryPath + ".old", notesDirectoryPath);  // Move Notes.old to Notes
                    File.Move(this.factory.DatabaseFile + ".old", this.factory.DatabaseFile);  // Move Knowte.db.old to Knowte.db
                }
                catch (Exception ex2)
                {
                    LogClient.Error("Could not restore original files. Exception: {0}", ex2.Message);
                }
            }

            return isSuccess;
        }
        #endregion
    }
}
