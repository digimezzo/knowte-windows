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
        private string applicationFolder = SettingsClient.ApplicationFolder();
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
                        var factory = new SQLiteConnectionFactory();
                        archive.CreateEntryFromFile(factory.DatabaseFile, Path.GetFileName(factory.DatabaseFile));
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
            // TODO: implement
            return false;
        }
        #endregion
    }
}
