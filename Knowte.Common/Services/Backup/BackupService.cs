using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Database;
using Knowte.Common.IO;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.Note;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
                    // Add the "Notes" subfolder    
                    var di = new DirectoryInfo(Path.Combine(ApplicationPaths.NoteStorageLocation, ApplicationPaths.NotesSubDirectory));
                    FileInfo[] fi = di.GetFiles();

                    foreach (FileInfo f in fi)
                    {
                        archive.CreateEntryFromFile(f.FullName, f.Directory.Name + "/" + f.Name);
                    }

                    // Add database file
                    archive.CreateEntryFromFile(this.factory.DatabaseFile, Path.GetFileName(this.factory.DatabaseFile));
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

        private async Task RestoreFromBackup(bool deleteCurrentNotes)
        {
            var backupFactory = new SQLiteConnectionFactory(this.BackupSubDirectory); // SQLiteConnectionFactory that points to the backup database file
            var backupCreator = new DbCreator(this.BackupSubDirectory); // DbCreator that points to the backup database file
            string currentNotesSubDirectoryPath = Path.Combine(ApplicationPaths.NoteStorageLocation, ApplicationPaths.NotesSubDirectory);
            string backupNotesSubDirectoryPath = Path.Combine(this.BackupSubDirectory, ApplicationPaths.NotesSubDirectory);

            List<Database.Entities.Notebook> currentNotebooks;
            List<Database.Entities.Note> currentNotes;

            List<Database.Entities.Notebook> backupNotebooks;
            List<Database.Entities.Note> backupNotes;

            await Task.Run(() =>
            {
                // Make sure the backup database is at the latest version
                if (backupCreator.DatabaseNeedsUpgrade()) backupCreator.UpgradeDatabase();

                // Get backup Notebooks and Notes
                using (var backupConn = backupFactory.GetConnection())
                {
                    backupNotebooks = backupConn.Table<Database.Entities.Notebook>().ToList();
                    backupNotes = backupConn.Table<Database.Entities.Note>().ToList();
                }

                // If required, delete all current Note files.
                if (deleteCurrentNotes)
                {
                    foreach (string file in Directory.GetFiles(currentNotesSubDirectoryPath, "*.xaml"))
                    {
                        File.Delete(file);
                    }
                }

                // Restore
                using (var currentConn = this.factory.GetConnection())
                {
                    // If required, delete all current Notebooks and Notes from the current database.
                    if (deleteCurrentNotes)
                    {
                        currentConn.Query<Database.Entities.Notebook>("DELETE FROM Notebook;");
                        currentConn.Query<Database.Entities.Note>("DELETE FROM Note;");
                    }

                    // Get current Notebooks and Notes
                    currentNotebooks = currentConn.Table<Database.Entities.Notebook>().ToList();
                    currentNotes = currentConn.Table<Database.Entities.Note>().ToList();

                    // Restore only the Notebooks that don't exist
                    foreach (Database.Entities.Notebook backupNotebook in backupNotebooks)
                    {
                        if (!currentNotebooks.Contains(backupNotebook)) currentConn.Insert(backupNotebook);
                    }

                    // Restore only the Notes that don't exist
                    foreach (Database.Entities.Note backupNote in backupNotes)
                    {
                        string backupNoteFile = Path.Combine(backupNotesSubDirectoryPath, backupNote.Id + ".xaml");

                        if (!currentNotes.Contains(backupNote) && File.Exists(backupNoteFile))
                        {
                            File.Copy(backupNoteFile, Path.Combine(currentNotesSubDirectoryPath, backupNote.Id + ".xaml"), true);
                            currentConn.Insert(backupNote);
                        }
                    }

                    // Fix links to missing notebooks
                    currentConn.Execute("UPDATE Note SET NotebookId = '' WHERE NotebookId NOT IN (SELECT Id FROM Notebook);");
                }
            });
        }

        private async Task MigrateAsync()
        {
            // TODO
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
                await this.RestoreFromBackup(deleteCurrentNotes);

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
                ResourceUtils.GetStringResource("Language_Backup"),
                ResourceUtils.GetStringResource("Language_Creating_Backup"),
                1000,
                () => this.BackupAsyncCallback(backupFile));

            return isSuccess;
        }

        public bool Import(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetStringResource("Language_Import"),
                ResourceUtils.GetStringResource("Language_Importing_Backup"),
                1000,
                () => this.RestoreAsyncCallback(backupFile, false));

            this.BackupRestored(this, new EventArgs());

            return isSuccess;
        }

        public bool Restore(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetStringResource("Language_Restore"),
                ResourceUtils.GetStringResource("Language_Restoring_Backup"),
                1000,
                () => this.RestoreAsyncCallback(backupFile, true));

            this.BackupRestored(this, new EventArgs());

            return isSuccess;
        }
        #endregion
    }
}
