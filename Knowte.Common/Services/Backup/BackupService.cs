using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Database;
using Knowte.Common.IO;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.Note;
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
        private INoteService noteService;
        private IDialogService dialogService;
        private string backupSubDirectory = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.BackupSubDirectory);
        #endregion

        #region Properties
        public string BackupSubDirectory
        {
            get { return this.backupSubDirectory; }
        }

        #endregion

        #region Construction
        public BackupService(INoteService noteService, IDialogService dialogService)
        {
            this.noteService = noteService;
            this.dialogService = dialogService;
            this.factory = new SQLiteConnectionFactory();

            // Initialize the Backup directory
            // -------------------------------

            // If the Backup subdirectory doesn't exist, create it.
            if (!Directory.Exists(this.BackupSubDirectory))
            {
                Directory.CreateDirectory(Path.Combine(this.BackupSubDirectory));
            }
        }
        #endregion

        #region Private
        private async Task CreateBackupFile(string backupFile)
        {
            await Task.Run(() =>
            {
                string tempFile = backupFile + ".temp";

                using (ZipArchive archive = ZipFile.Open(tempFile, ZipArchiveMode.Create))
                {
                    // Add the files from the storage location
                    var di = new DirectoryInfo(ApplicationPaths.CurrentNoteStorageLocation);
                    FileInfo[] fi = di.GetFiles();

                    foreach (FileInfo f in fi)
                    {
                        archive.CreateEntryFromFile(f.FullName, f.Name);
                    }
                }

                if (File.Exists(backupFile)) File.Delete(backupFile);
                File.Move(tempFile, backupFile);
            });
        }

        private async Task<bool> BackupAsyncCallback(string backupFile)
        {
            if (string.IsNullOrWhiteSpace(backupFile))
            {
                LogClient.Error("Could not perform backup: backupFile is empty.");
                return false;
            }

            bool isSuccess = true;

            try
            {
                await this.CreateBackupFile(backupFile);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform backup. Exception: {0}", ex.Message);
            }

            return isSuccess;
        }

        private async Task CleanBackupDirectoryAsync()
        {
            await Task.Run(() =>
            {
                DirectoryInfo di = new DirectoryInfo(this.BackupSubDirectory);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            });
        }

        private async Task ExtractBackupFileToBackupDirectory(string backupFile)
        {
            await Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(backupFile, this.BackupSubDirectory);
            });
        }

        private async Task<bool> RestoreAsyncCallback(string backupFile, bool deleteCurrentNotes)
        {
            bool isSuccess = true;

            try
            {
                // Clean the backup directory (In case it wasn't cleaned during the last restore, because of a crash for example.)
                await this.CleanBackupDirectoryAsync();

                // Extract the backup file to the Backup directory
                await this.ExtractBackupFileToBackupDirectory(backupFile);

                // Restore from backup
                await this.noteService.MigrateAsync(this.BackupSubDirectory, deleteCurrentNotes);

                // Clean the backup directory
                await this.CleanBackupDirectoryAsync();
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform restore. Exception: {0}", ex.Message);
            }

            return isSuccess;
        }
        #endregion

        #region IBackupService
        public event EventHandler BackupRestored = delegate { };

        public bool Backup(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetString("Language_Backup"),
                ResourceUtils.GetString("Language_Creating_Backup"),
                1000,
                () => this.BackupAsyncCallback(backupFile));

            return isSuccess;
        }

        public bool Import(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetString("Language_Import"),
                ResourceUtils.GetString("Language_Importing_Backup"),
                1000,
                () => this.RestoreAsyncCallback(backupFile, false));

            this.BackupRestored(this, new EventArgs());

            return isSuccess;
        }

        public bool Restore(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetString("Language_Restore"),
                ResourceUtils.GetString("Language_Restoring_Backup"),
                1000,
                () => this.RestoreAsyncCallback(backupFile, true));

            this.BackupRestored(this, new EventArgs());

            return isSuccess;
        }
        #endregion
    }
}
