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
                await this.Migrate(this.BackupSubDirectory, deleteCurrentNotes);

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

        public async Task Migrate(string sourceFolder, bool deleteDestination)
        {
            var sourceFactory = new SQLiteConnectionFactory(sourceFolder); // SQLiteConnectionFactory that points to the source database file
            var sourceCreator = new DbCreator(sourceFolder); // DbCreator that points to the source database file
            string sourceNotesSubDirectoryPath = Path.Combine(sourceFolder, ApplicationPaths.NotesSubDirectory);
            string destinationNotesSubDirectoryPath = Path.Combine(ApplicationPaths.NoteStorageLocation, ApplicationPaths.NotesSubDirectory);

            List<Database.Entities.Notebook> sourceNotebooks;
            List<Database.Entities.Note> sourceNotes;
            List<Database.Entities.Notebook> destinationNotebooks;
            List<Database.Entities.Note> destinationNotes;

            await Task.Run(() =>
            {
                // Make sure the source database is at the latest version
                if (sourceCreator.DatabaseNeedsUpgrade()) sourceCreator.UpgradeDatabase();

                // Get source Notebooks and Notes
                using (var sourceConn = sourceFactory.GetConnection())
                {
                    sourceNotebooks = sourceConn.Table<Database.Entities.Notebook>().ToList();
                    sourceNotes = sourceConn.Table<Database.Entities.Note>().ToList();
                }

                // If required, delete all destination Note files.
                if (deleteDestination)
                {
                    foreach (string file in Directory.GetFiles(destinationNotesSubDirectoryPath, "*.xaml"))
                    {
                        File.Delete(file);
                    }
                }

                // Restore
                using (var destinationConn = this.factory.GetConnection())
                {
                    // If required, delete all Notebooks and Notes from the destination database.
                    if (deleteDestination)
                    {
                        destinationConn.Query<Database.Entities.Notebook>("DELETE FROM Notebook;");
                        destinationConn.Query<Database.Entities.Note>("DELETE FROM Note;");
                    }

                    // Get destination Notebooks and Notes
                    destinationNotebooks = destinationConn.Table<Database.Entities.Notebook>().ToList();
                    destinationNotes = destinationConn.Table<Database.Entities.Note>().ToList();

                    // Restore only the Notebooks that don't exist
                    foreach (Database.Entities.Notebook sourceNotebook in sourceNotebooks)
                    {
                        if (!destinationNotebooks.Contains(sourceNotebook)) destinationConn.Insert(sourceNotebook);
                    }

                    // Restore only the Notes that don't exist
                    foreach (Database.Entities.Note sourceNote in sourceNotes)
                    {
                        string sourceNoteFile = Path.Combine(sourceNotesSubDirectoryPath, sourceNote.Id + ".xaml");

                        if (!destinationNotes.Contains(sourceNote) && File.Exists(sourceNoteFile))
                        {
                            File.Copy(sourceNoteFile, Path.Combine(destinationNotesSubDirectoryPath, sourceNote.Id + ".xaml"), true);
                            destinationConn.Insert(sourceNote);
                        }
                    }

                    // Fix links to missing notebooks
                    destinationConn.Execute("UPDATE Note SET NotebookId = '' WHERE NotebookId NOT IN (SELECT Id FROM Notebook);");
                }
            });
        }

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
