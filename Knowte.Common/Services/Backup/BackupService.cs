using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Database;
using Knowte.Common.Database.Entities;
using Knowte.Common.IO;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.Note;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Knowte.Common.Services.Backup
{
    public class BackupService : IBackupService
    {
        public class NotebookJson
        {
            public string Name { get; set; }

            public string CreationDate { get; set; }
        }

        public class NoteJson
        {
            public string Title { get; set; }

            public string Text { get; set; }

            public string Notebook { get; set; }

            public bool IsMarked { get; set; }

            public string CreationDate { get; set; }

            public string ModificationDate { get; set; }
        }

        private SQLiteConnectionFactory factory;
        private INoteService noteService;
        private IDialogService dialogService;
        private string backupSubDirectory = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.BackupSubDirectory);

        public string BackupSubDirectory
        {
            get { return this.backupSubDirectory; }
        }

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

        public event EventHandler BackupRestored = delegate { };


        public bool Export(string exportLocation)
        {
            bool isSuccess = true;

            try
            {
                string storageLocation = ApplicationPaths.CurrentNoteStorageLocation;

                var factory = new SQLiteConnectionFactory(storageLocation);
                string notesDatabaseFile = factory.DatabaseFile;

                // Get notebooks
                List<Notebook> notebooks = null;

                using (var conn = factory.GetConnection())
                {
                    notebooks = conn.Query<Notebook>("SELECT * FROM Notebook;");
                }

                // Export notebooks
                var notebooksJson = new List<NotebookJson>();

                foreach (Notebook notebook in notebooks)
                {
                    var notebookJson = new NotebookJson();
                    notebookJson.Name = notebook.Title;
                    notebookJson.CreationDate = new DateTime(notebook.CreationDate).ToString("yyyy-MM-dd hh:mm:ss");
                    notebooksJson.Add(notebookJson);
                }

                string notebooksJsonString = JsonConvert.SerializeObject(notebooksJson);

                // Get notes
                List<Database.Entities.Note> notes = null;

                using (var conn = factory.GetConnection())
                {
                    notes = conn.Query<Database.Entities.Note>("SELECT * FROM Note;");
                }

                // Export notes
                var notesJson = new List<NoteJson>();

                foreach (Database.Entities.Note note in notes)
                {
                    Notebook notebook = notebooks.Where(x => x.Id.Equals(note.NotebookId)).FirstOrDefault();
                    string notebookTitle = string.Empty;

                    if (notebook != null)
                    {
                        notebookTitle = notebook.Title;
                    }

                    char tab = '\u0009';

                    var noteJson = new NoteJson();
                    noteJson.Title = note.Title;

                    noteJson.Text = note.Text;
                    noteJson.Text = noteJson.Text.Replace("•", "-");
                    noteJson.Text = noteJson.Text.Replace('"', '\"');

                    noteJson.IsMarked = note.Flagged == 0 ? false : true;

                    noteJson.Notebook = notebookTitle;
                    noteJson.CreationDate = new DateTime(note.CreationDate).ToString("yyyy-MM-dd hh:mm:ss");
                    noteJson.ModificationDate = new DateTime(note.ModificationDate).ToString("yyyy-MM-dd hh:mm:ss");
                    notesJson.Add(noteJson);
                }

                string notesJsonString = JsonConvert.SerializeObject(notesJson);

                // Write to files
                File.WriteAllText(Path.Combine(exportLocation, "Notebooks.json"), notebooksJsonString);
                File.WriteAllText(Path.Combine(exportLocation, "Notes.json"), notesJsonString);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("Could not perform export. Exception: {0}", ex.Message);
            }

            return isSuccess;
        }

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
    }
}
