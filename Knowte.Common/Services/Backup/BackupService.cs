using Digimezzo.Utilities.Log;
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
        #endregion

        #region Construction
        public BackupService(INoteService noteService, IDialogService dialogService)
        {
            this.noteService = noteService;
            this.dialogService = dialogService;
            this.factory = new SQLiteConnectionFactory();
        }
        #endregion

        #region Private
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
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform backup. Exception: {0}", ex.Message);
            }

            return isSuccess;
        }

        private async Task<bool> RestoreAsyncCallback(string backupFile, bool isImport)
        {
            if (string.IsNullOrWhiteSpace(backupFile))
            {
                LogClient.Error("Could not perform restore: backupFile is empty.");
                return false;
            }

            bool isSuccess = true;

            string notesDirectoryPath = Path.Combine(ApplicationPaths.NoteStorageLocation, ApplicationPaths.NotesSubDirectory);

            try
            {
                // Close all note windows
                this.noteService.CloseAllNoteWindows();

                await Task.Run(() =>
                {
                    if (Directory.Exists(notesDirectoryPath + ".old")) Directory.Delete(notesDirectoryPath + ".old", true); // Delete Knowte.db.old
                    if (File.Exists(this.factory.DatabaseFile + ".old")) File.Delete(this.factory.DatabaseFile + ".old"); // Delete Notes.old

                    Directory.Move(notesDirectoryPath, notesDirectoryPath + ".old"); // Move Notes directory to Notes.old
                    File.Move(this.factory.DatabaseFile, this.factory.DatabaseFile + ".old"); // Move Knowte.db to Knowte.db.old.

                    // Restore backup
                    ZipFile.ExtractToDirectory(backupFile, ApplicationPaths.NoteStorageLocation);

                    Directory.Delete(notesDirectoryPath + ".old", true); // Delete Notes.old
                    File.Delete(this.factory.DatabaseFile + ".old"); // Delete Knowte.db.old
                });
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform restore. Exception: {0}", ex.Message);

                try
                {
                    await Task.Run(() =>
                    {
                        // If restore fails, restore from .old files
                        LogClient.Error("Trying to restore original files.");

                        if (File.Exists(this.factory.DatabaseFile)) File.Delete(this.factory.DatabaseFile); // Delete Knowte.db
                        if (Directory.Exists(notesDirectoryPath)) Directory.Delete(notesDirectoryPath, true); // Delete Notes

                        Directory.Move(notesDirectoryPath + ".old", notesDirectoryPath);  // Move Notes.old to Notes
                        File.Move(this.factory.DatabaseFile + ".old", this.factory.DatabaseFile);  // Move Knowte.db.old to Knowte.db
                    });
                }
                catch (Exception ex2)
                {
                    LogClient.Error("Could not restore original files. Exception: {0}", ex2.Message);
                }
            }

            this.BackupRestored(this, new EventArgs());

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
                () => this.RestoreAsyncCallback(backupFile, true));

            return isSuccess;
        }

        public bool Restore(string backupFile)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
                null,
                ResourceUtils.GetStringResource("Language_Restore"), 
                ResourceUtils.GetStringResource("Language_Restoring_Backup"), 
                1000, 
                () => this.RestoreAsyncCallback(backupFile, false));

            return isSuccess;
        }
        #endregion
    }
}
