using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Ionic.Zip;
using Knowte.Common.Base;
using Knowte.Common.Controls;
using Knowte.Common.Database;
using Knowte.Common.Database.Entities;
using Knowte.Common.Extensions;
using Knowte.Common.IO;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;

namespace Knowte.Common.Services.Note
{
    public class NoteService : INoteService
    {
        #region Variables
        private IDialogService dialogService;
        private SQLiteConnectionFactory factory;
        #endregion

        #region Construction
        public NoteService(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.factory = new SQLiteConnectionFactory();

            // Initialize the notes storage location
            // -------------------------------------

            // If using the default storage location, check if the location needs to be created.
            if(ApplicationPaths.IsUsingDefaultStorageLocation)
            {
                if (!Directory.Exists(ApplicationPaths.CurrentNoteStorageLocation))
                {
                    Directory.CreateDirectory(ApplicationPaths.CurrentNoteStorageLocation);
                }
            }
        }
        #endregion

        #region Private
        private async Task InitializeStorageIfRequiredAsync(string newLocation)
        {
            await Task.Run(() =>
            {
                // Database file
                var migrator = new DbMigrator(newLocation);

                if (migrator.DatabaseExists())
                {
                    migrator.UpgradeDatabase();
                }
                else
                {
                    migrator.InitializeNewDatabase();
                }

                // Notes directory
                string notesDirectoryPath = newLocation;
                if (!Directory.Exists(notesDirectoryPath)) Directory.CreateDirectory(notesDirectoryPath);
            });
        }
        #endregion

        #region INoteService
        public event FlagUpdatedEventHandler FlagUpdated = delegate { };
        public event EventHandler StorageLocationChanged = delegate { };
        public event EventHandler NotesChanged = delegate { };

        public void OnNotesChanged()
        {
            this.NotesChanged(this, new EventArgs());
        }

        public string GetUniqueNoteTitle(string proposedTitle)
        {
            List<string> similarTitles;

            int counter = 1;
            string uniqueTitle = proposedTitle + " " + counter.ToString();

            using (var conn = this.factory.GetConnection())
            {
                similarTitles = conn.Table<Database.Entities.Note>().ToList().Where(n => n.Title.StartsWith(proposedTitle)).OrderBy(n => n.Title).Select(n => n.Title).ToList();
            }

            while (similarTitles.Contains(uniqueTitle))
            {
                counter++;
                uniqueTitle = proposedTitle + " " + counter.ToString();
            }

            return uniqueTitle;
        }
        public async Task MigrateAsync(string sourceFolder, bool deleteDestination)
        {
            var sourceFactory = new SQLiteConnectionFactory(sourceFolder); // SQLiteConnectionFactory that points to the source database file
            var sourceMigrator = new DbMigrator(sourceFolder); // DbMigrator that points to the source database file

            List<Notebook> sourceNotebooks;
            List<Database.Entities.Note> sourceNotes;

            await Task.Run(() =>
            {
                // Make sure the source database is at the latest version
                if (sourceMigrator.DatabaseNeedsUpgrade()) sourceMigrator.UpgradeDatabase();

                // Get source Notebooks and Notes
                using (var sourceConn = sourceFactory.GetConnection())
                {
                    sourceNotebooks = sourceConn.Table<Notebook>().ToList();
                    sourceNotes = sourceConn.Table<Database.Entities.Note>().ToList();
                }

                // If required, delete all destination Note files.
                if (deleteDestination)
                {
                    foreach (string file in Directory.GetFiles(ApplicationPaths.CurrentNoteStorageLocation, "*.xaml"))
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
                        destinationConn.Query<Notebook>("DELETE FROM Notebook;");
                        destinationConn.Query<Database.Entities.Note>("DELETE FROM Note;");
                    }

                    // Get destination Notebook information
                    List<string> destinationNotebookIds = destinationConn.Table<Notebook>().ToList().Select(n => n.Id).ToList();
                    List<string> destinationNotebookTitles = destinationConn.Table<Notebook>().ToList().Select(n => n.Title).ToList();

                    // Restore only the Notebooks that don't exist (Id AND Title can't exist at destination. Both need to be unique).
                    foreach (Notebook sourceNotebook in sourceNotebooks)
                    {
                        if (!destinationNotebookIds.Contains(sourceNotebook.Id) && !destinationNotebookTitles.Contains(sourceNotebook.Title))
                        {
                            destinationConn.Insert(sourceNotebook);
                        }
                    }

                    // Get destination note information
                    List<string> destinationNoteIds = destinationConn.Table<Database.Entities.Note>().ToList().Select(n => n.Id).ToList();
                    List<string> destinationNoteTitles = destinationConn.Table<Database.Entities.Note>().ToList().Select(n => n.Title).ToList();

                    // Restore only the Notes which don't exist (Id AND Title can't exist at destination. Both need to be unique).
                    foreach (Database.Entities.Note sourceNote in sourceNotes)
                    {
                        string sourceNoteFile = Path.Combine(sourceFolder, sourceNote.Id + ".xaml");

                        if (!destinationNoteIds.Contains(sourceNote.Id) && !destinationNoteTitles.Contains(sourceNote.Title))
                        {
                            File.Copy(sourceNoteFile, Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, sourceNote.Id + ".xaml"), true);
                            destinationConn.Insert(sourceNote);
                        }
                    }

                    // Clear links to missing notebooks
                    destinationConn.Execute("UPDATE Note SET NotebookId = '' WHERE NotebookId NOT IN (SELECT Id FROM Notebook);");
                }
            });
        }

        public async Task<bool> ChangeStorageLocationAsyncCallback(string newStorageLocation, bool moveCurrentNotes)
        {
            bool isSuccess = true;

            try
            {
                string oldStorageLocation = SettingsClient.Get<string>("General", "NoteStorageLocation");
                SettingsClient.Set<string>("General", "NoteStorageLocation", newStorageLocation);

                await this.InitializeStorageIfRequiredAsync(newStorageLocation);
                if (moveCurrentNotes) await this.MigrateAsync(oldStorageLocation, false);
            }
            catch (Exception ex)
            {
                isSuccess = false;
                LogClient.Error("An error occurred while changing the storage location. MoveCurrentNotes={0}. Exception: {1}", moveCurrentNotes.ToString(), ex.Message);
            }

            this.StorageLocationChanged(this, new EventArgs());

            return isSuccess;
        }

        public async Task<bool> ChangeStorageLocationAsync(string newStorageLocation, bool moveCurrentNotes)
        {
            bool isSuccess = this.dialogService.ShowBusyDialog(
               null,
               ResourceUtils.GetStringResource("Language_Backup"),
               ResourceUtils.GetStringResource("Language_Changing_Storage_Location"),
               500,
               () => this.ChangeStorageLocationAsyncCallback(newStorageLocation, moveCurrentNotes));

            return isSuccess;   
        }

        public void CloseAllNoteWindows()
        {
            foreach (Window win in Application.Current.Windows)
            {
                if (win is KnowteWindow && !((KnowteWindow)win).IsMainWindow)
                {
                    win.Close();
                }
            }
        }

        public void NewNotebook(Notebook notebook)
        {
            using (var conn = this.factory.GetConnection())
            {
                conn.Insert(notebook);
            }
        }

        public void DeleteNotebook(String id)
        {
            using (var conn = this.factory.GetConnection())
            {
                Notebook notebookToDelete = conn.Table<Notebook>().Where((nb) => nb.Id == id).FirstOrDefault();

                // Delete Notebook from database
                if (notebookToDelete != null)
                {
                    conn.Delete(notebookToDelete);
                }

                // Clear NotebookId for Notes which are in this Notebook
                List<Database.Entities.Note> notesToUpdate = conn.Table<Database.Entities.Note>().Where((n) => n.NotebookId == id).ToList();

                if (notesToUpdate != null & notesToUpdate.Count > 0)
                {
                    foreach (Database.Entities.Note noteToUpdate in notesToUpdate)
                    {
                        noteToUpdate.NotebookId = string.Empty;
                    }

                    conn.UpdateAll(notesToUpdate);
                }
            }
        }

        public List<Notebook> GetNotebooks(ref int totalNotebooks)
        {
            List<Notebook> notebooks = null;

            using (var conn = this.factory.GetConnection())
            {
                // SQLite.Net doesn't support ToLower(), so we just use a query.
                notebooks = conn.Query<Notebook>("SELECT * FROM Notebook ORDER BY LOWER(Title);");
                totalNotebooks = notebooks.Count();
            }

            return notebooks;
        }

        public List<Notebook> GetNotebooks()
        {
            int dummyInt = 0;

            return this.GetNotebooks(ref dummyInt);
        }

        public bool NotebookExists(Notebook notebook)
        {
            int count = 0;

            using (var conn = this.factory.GetConnection())
            {
                count = conn.Table<Notebook>().Where((nb) => nb.Title == notebook.Title).Count();
            }

            return count > 0;
        }

        public void UpdateNotebook(string id, string newTitle)
        {
            using (var conn = this.factory.GetConnection())
            {
                Notebook notebookToUpdate = conn.Table<Notebook>().Where((nb) => nb.Id == id).FirstOrDefault();

                if (notebookToUpdate != null)
                {
                    notebookToUpdate.Title = newTitle;
                    conn.Update(notebookToUpdate);
                }
            }
        }

        public String GetNotebookId(string notebookTitle)
        {
            string notebookId = null;

            using (var conn = this.factory.GetConnection())
            {
                notebookId = conn.Table<Notebook>().Select((nb) => nb).Where((nb) => nb.Title == notebookTitle).ToList().Select((nb) => nb.Id).FirstOrDefault();
            }

            return notebookId;
        }

        public Notebook GetNotebook(string id)
        {
            Notebook requestedNotebook = null;

            using (var conn = this.factory.GetConnection())
            {
                requestedNotebook = conn.Table<Notebook>().Where((nb) => nb.Id == id).FirstOrDefault();
            }

            if (requestedNotebook == null)
            {
                requestedNotebook = new Notebook { Id = "1", Title = "Unfiled notes", CreationDate = DateTime.Now.Ticks, IsDefaultNotebook = true };
            }

            return requestedNotebook;
        }

        public void NewNote(FlowDocument document, string id, string title, string notebookId)
        {
            DateTime saveDate = DateTime.Now;

            if (notebookId == null) notebookId = "";

            TextRange tr = new TextRange(document.ContentStart, document.ContentEnd);
            string text = "";

            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, System.Windows.DataFormats.Text);
                text = tr.Text;
            }

            // Save FlowDocument as a xaml file
            using (FileStream fs = new FileStream(System.IO.Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, id + ".xaml"), FileMode.Create))
            {
                tr.Save(fs, System.Windows.DataFormats.XamlPackage);
                fs.Close();
            }

            // Add Note to database
            var newNote = new Database.Entities.Note
            {
                Id = id,
                NotebookId = notebookId,
                Title = title,
                Text = text,
                CreationDate = saveDate.Ticks,
                OpenDate = saveDate.Ticks,
                ModificationDate = saveDate.Ticks,
                Width = Defaults.DefaultNoteWidth,
                Height = Defaults.DefaultNoteHeight,
                Top = Defaults.DefaultNoteTop,
                Left = Defaults.DefaultNoteLeft,
                Maximized = 0,
                Flagged = 0
            };

            using (var conn = this.factory.GetConnection())
            {
                conn.Insert(newNote);
            }
        }

        public void UpdateOpenDate(string id)
        {
            using (var conn = this.factory.GetConnection())
            {
                Database.Entities.Note noteToUpdate = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();

                if (noteToUpdate != null)
                {
                    noteToUpdate.OpenDate = DateTime.Now.Ticks;
                    conn.Update(noteToUpdate);
                }
            }
        }

        public void UpdateNoteParameters(string id, double width, double height, double top, double left, bool maximized)
        {
            using (var conn = this.factory.GetConnection())
            {
                Database.Entities.Note noteToUpdate = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();

                if (noteToUpdate != null)
                {
                    if (!maximized)
                    {
                        noteToUpdate.Width = Convert.ToInt64(width);
                        noteToUpdate.Height = Convert.ToInt64(height);
                        noteToUpdate.Top = Convert.ToInt64(top);
                        noteToUpdate.Left = Convert.ToInt64(left);
                    }

                    noteToUpdate.Maximized = maximized ? 1 : 0;

                    conn.Update(noteToUpdate);
                }
            }
        }

        public void UpdateNoteFlag(string id, bool flagged)
        {
            using (var conn = this.factory.GetConnection())
            {
                Database.Entities.Note noteToUpdate = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();

                if (noteToUpdate != null)
                {
                    noteToUpdate.Flagged = flagged ? 1 : 0;

                    conn.Update(noteToUpdate);

                    this.FlagUpdated(noteToUpdate.Id, flagged);
                }
            }
        }

        public void UpdateNote(FlowDocument document, string id, string title, string notebookId, double width, double height, double top, double left, bool maximized)
        {
            DateTime modificationDate = DateTime.Now;

            if (notebookId == null)
            {
                notebookId = "";
            }

            TextRange tr = new TextRange(document.ContentStart, document.ContentEnd);

            string text = "";

            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, System.Windows.DataFormats.Text);
                text = tr.Text;
            }

            // Save FlowDocument as a xaml file
            FileStream f = new FileStream(System.IO.Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, id + ".xaml"), FileMode.Create);
            tr.Save(f, System.Windows.DataFormats.XamlPackage);
            f.Close();

            // Update Note to database
            using (var conn = this.factory.GetConnection())
            {
                Database.Entities.Note noteToUpdate = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();

                if (noteToUpdate != null)
                {
                    noteToUpdate.Title = title;
                    noteToUpdate.ModificationDate = modificationDate.Ticks;
                    noteToUpdate.NotebookId = notebookId;
                    noteToUpdate.Text = text;

                    if (!maximized)
                    {
                        noteToUpdate.Width = Convert.ToInt64(width);
                        noteToUpdate.Height = Convert.ToInt64(height);
                        noteToUpdate.Top = Convert.ToInt64(top);
                        noteToUpdate.Left = Convert.ToInt64(left);
                    }

                    noteToUpdate.Maximized = maximized ? 1 : 0;

                    conn.Update(noteToUpdate);
                }
            }

            this.OnNotesChanged();
        }

        public LoadNoteResult LoadNote(FlowDocument doc, Database.Entities.Note note)
        {
            LoadNoteResult result = LoadNoteResult.Success;

            try
            {
                TextRange t = new TextRange(doc.ContentStart, doc.ContentEnd);
                FileStream f = new FileStream(System.IO.Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, note.Id + ".xaml"), FileMode.Open);
                t.Load(f, System.Windows.DataFormats.XamlPackage);
                f.Close();
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not load the note. Exception: {0}", ex.Message);
                result = LoadNoteResult.Error;
            }

            return result;
        }

        public Database.Entities.Note GetNote(string title)
        {
            Database.Entities.Note requestedNote = null;

            using (var conn = this.factory.GetConnection())
            {
                requestedNote = conn.Table<Database.Entities.Note>().Where((n) => n.Title == title).FirstOrDefault();
            }
            return requestedNote;
        }

        public Database.Entities.Note GetNoteById(string id)
        {
            Database.Entities.Note requestedNote = null;

            using (var conn = this.factory.GetConnection())
            {
                requestedNote = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();
            }
            return requestedNote;
        }

        public async Task DeleteNoteAsync(string id)
        {
            await Task.Run(() => {
                using (var conn = this.factory.GetConnection())
                {
                    Database.Entities.Note noteToDelete = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).FirstOrDefault();

                    if (noteToDelete != null)
                    {
                        // Delete Note from database
                        conn.Delete(noteToDelete);

                        // Delete Note from disk
                        try
                        {
                            File.Delete(System.IO.Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, id + ".xaml"));
                        }
                        catch (Exception)
                        {
                        }

                        this.OnNotesChanged();
                    }
                }
            });
        }

        public bool NoteExists(string title)
        {
            int count = 0;

            using (var conn = this.factory.GetConnection())
            {
                count = conn.Table<Database.Entities.Note>().Where((n) => n.Title == title).Count();
            }

            return count > 0;
        }

        public bool NoteIdExists(string id)
        {
            int count = 0;

            using (var conn = this.factory.GetConnection())
            {
                count = conn.Table<Database.Entities.Note>().Where((n) => n.Id == id).Count();
            }

            return count > 0;
        }

        public List<Database.Entities.Note> GetRecentlyOpenedNotes(int number)
        {
            List<Database.Entities.Note> recentlyOpenedNotes = null;

            using (var conn = this.factory.GetConnection())
            {
                recentlyOpenedNotes = conn.Table<Database.Entities.Note>().OrderByDescending((n) => n.OpenDate).Take(number).ToList();
            }

            return recentlyOpenedNotes;
        }

        public List<Database.Entities.Note> GetFlaggedNotes()
        {
            List<Database.Entities.Note> flaggedNotes = null;

            using (var conn = this.factory.GetConnection())
            {
                flaggedNotes = conn.Table<Database.Entities.Note>().Where((n) => n.Flagged == 1).OrderByDescending((n) => n.OpenDate).ToList();
            }

            return flaggedNotes;
        }

        public List<Database.Entities.Note> GetNotes(Notebook notebook, string searchString, ref int count, bool orderByLastChanged, string noteFilter)
        {
            string[] search = searchString.Split(new char[] { ' ' });

            List<Database.Entities.Note> notes = null;

            using (var conn = this.factory.GetConnection())
            {
                // First, get all the notes
                notes = conn.Table<Database.Entities.Note>().ToList();

                switch (noteFilter)
                {
                    case NoteFilters.Today:
                        notes = notes.Where(n => DateUtils.CountDays(new DateTime(n.ModificationDate), DateTime.Now) == 0).ToList();
                        break;
                    case NoteFilters.Yesterday:
                        notes = notes.Where(n => DateUtils.CountDays(new DateTime(n.ModificationDate), DateTime.Now.AddDays(-1)) == 0).ToList();
                        break;
                    case NoteFilters.ThisWeek:
                        notes = notes.Where(n => DateUtils.CountDays(new DateTime(n.ModificationDate), DateTime.Now) <= (int)DateTime.Now.DayOfWeek).ToList();
                        break;
                    case NoteFilters.Flagged:
                        notes = notes.Where(n => n.Flagged == 1).ToList();
                        break;
                    case NoteFilters.All:
                        break;
                        // do not filter
                }

                // Then, add a WHERE clause
                if (notebook.Id.Equals("0"))
                {
                    // Get all the notes
                    notes = notes.Where(n => search.All(s => n.Title.ToLower().Contains(s.ToLower()) | n.Text.ToLower().Contains(s.ToLower()))).ToList();
                }
                else if (notebook.Id.Equals("1"))
                {
                    // Get only the notes without notebook id
                    notes = notes.Where(n => n.NotebookId.Equals("") & (search.All(s => n.Title.ToLower().Contains(s.ToLower()) | n.Text.ToLower().Contains(s.ToLower())))).ToList();
                }
                else
                {
                    // Get only the notes for the selected notebook id
                    notes = notes.Where(n => n.NotebookId.Equals(notebook.Id) & (search.All(s => n.Title.ToLower().Contains(s.ToLower()) | n.Text.ToLower().Contains(s.ToLower())))).ToList();
                }

                // Finally, ORDER BY
                if (!orderByLastChanged)
                {
                    // Order alpabetically
                    notes = notes.OrderBy(n => n.Title).ToList();
                }
                else
                {
                    // Order by last changed
                    notes = notes.OrderByDescending(n => n.ModificationDate).ToList();
                }

                count = notes.Count();
            }

            return notes;
        }

        public void CountNotes(ref int allNotesCount, ref int todayNotesCount, ref int yesterdayNotesCount, ref int thisWeekNotesCount, ref int flaggedNotesCount)
        {
            allNotesCount = 0;
            todayNotesCount = 0;
            yesterdayNotesCount = 0;
            thisWeekNotesCount = 0;
            flaggedNotesCount = 0;

            List<Database.Entities.Note> notes = null;

            using (var conn = this.factory.GetConnection())
            {
                // First, get all the notes
                notes = conn.Table<Database.Entities.Note>().ToList();

                foreach (Database.Entities.Note note in notes)
                {
                    // All notes
                    allNotesCount += 1;

                    // Today
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now) == 0)
                    {
                        todayNotesCount += 1;
                    }

                    // Yesterday
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now.AddDays(-1)) == 0)
                    {
                        yesterdayNotesCount += 1;
                    }

                    // This week
                    if (DateUtils.CountDays(new DateTime(note.ModificationDate), DateTime.Now) <= (int)DateTime.Now.DayOfWeek)
                    {
                        thisWeekNotesCount += 1;
                    }

                    // Flagged
                    if (note.Flagged == 1)
                    {
                        flaggedNotesCount += 1;
                    }
                }
            }
        }

        public void ExportToRtf(string id, string title, string fileName)
        {
            FlowDocument mergedDocument = this.MergeDocument(id, title);

            TextRange tr = new TextRange(mergedDocument.ContentStart, mergedDocument.ContentEnd);

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                tr.Save(fs, System.Windows.DataFormats.Rtf);
                fs.Close();
            }
        }

        public void Print(string id, string title)
        {
            // Copy the flowdocument to an offscreen copy (prevents changes in the note window due to printing)
            FlowDocument mergedDocument = this.MergeDocument(id, title);

            // The printing
            PrintDialog pd = new PrintDialog();
            mergedDocument.PageHeight = pd.PrintableAreaHeight;
            mergedDocument.PageWidth = pd.PrintableAreaWidth;
            mergedDocument.PagePadding = new Thickness(50);
            mergedDocument.ColumnGap = 0;
            mergedDocument.ColumnWidth = pd.PrintableAreaWidth;

            IDocumentPaginatorSource dps = mergedDocument;

            if ((bool)pd.ShowDialog())
            {
                pd.PrintDocument(dps.DocumentPaginator, title.SanitizeFilename());
            }
        }

        public FlowDocument MergeDocument(string id, string title)
        {
            FlowDocument mergedDocument = new FlowDocument();

            Run run = new Run(title);
            Paragraph par = new Paragraph { Margin = new Thickness(0, 0, 0, 20) };
            par.FontSize = 24;
            par.FontFamily = new FontFamily(Defaults.NoteFont);
            par.Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(Defaults.PrintTitleColor) };
            Run run2 = new Run("");
            Paragraph par2 = new Paragraph();

            par.Inlines.Add(run);
            par2.Inlines.Add(run2);
            mergedDocument.Blocks.Add(par);
            mergedDocument.Blocks.Add(par2);

            FlowDocument tempDoc = new FlowDocument();
            LoadNote(tempDoc, new Database.Entities.Note { Id = id });

            var hyperlinks = VisualTreeUtils.GetVisuals(tempDoc).OfType<Hyperlink>();

            foreach (Hyperlink link in hyperlinks)
            {
                link.Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(Defaults.PrintLinkColor) };
            }

            TextRange trTemp = new TextRange(tempDoc.ContentStart, tempDoc.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                trTemp.Save(ms, System.Windows.DataFormats.Rtf);

                TextRange trMerged = new TextRange(mergedDocument.ContentEnd, mergedDocument.ContentEnd);
                trMerged.Load(ms, System.Windows.DataFormats.Rtf);

                ms.Close();
            }

            return mergedDocument;
        }

        public void ExportFile(string noteId, string filename)
        {
            // First, we set some defaults
            string tempPath = System.IO.Path.GetTempPath();
            string inputXamlFile = System.IO.Path.Combine(ApplicationPaths.CurrentNoteStorageLocation, noteId + ".xaml");
            string outputXamlFile = System.IO.Path.Combine(tempPath, noteId + ".xaml");
            string outputXmlFile = System.IO.Path.Combine(tempPath, noteId + ".xml");
            string zipFileName = System.IO.Path.Combine(tempPath, noteId + ".zip");

            // Then, we copy the xaml file of the note to the temporary directory
            System.IO.File.Copy(inputXamlFile, outputXamlFile, true);

            // Then, we create a small XML file containing the metadata

            Database.Entities.Note note = GetNoteById(noteId); // Gets the note details

            if (!File.Exists(outputXmlFile))
            {
                XDocument xml = XDocument.Parse("<Meta></Meta>");
                xml.Save(outputXmlFile);
            }

            var exportDate = DateTime.Now;

            XDocument xmlFile = XDocument.Load(outputXmlFile);
            xmlFile.Element("Meta").Add(new XElement("Note", new XAttribute("Id", note.Id), new XAttribute("NotebookId", note.NotebookId), new XAttribute("Title", note.Title), new XAttribute("CreationDate", note.CreationDate), new XAttribute("OpenDate", note.OpenDate), new XAttribute("ModificationDate", note.ModificationDate), new XAttribute("Flagged", note.Flagged), new XAttribute("Width", note.Width), new XAttribute("Height", note.Height),
            new XAttribute("Top", note.Top), new XAttribute("Left", note.Left), new XAttribute("Maximized", note.Maximized)));
            xmlFile.Save(outputXmlFile);

            // Then, zip xaml and XML file
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(outputXamlFile, "");
                zip.AddFile(outputXmlFile, "");
                zip.Save(zipFileName);

                if (System.IO.File.Exists(filename))
                {
                    System.IO.File.Delete(filename);
                }

                System.IO.File.Move(System.IO.Path.Combine(tempPath, zipFileName), filename);
            }

            System.IO.File.Delete(outputXamlFile);
            System.IO.File.Delete(outputXmlFile);
        }

        public void ImportFile(string filename)
        {
            // Some default paths
            string tempPath = System.IO.Path.GetTempPath();
            string zippedGuid = "";

            // Unzip
            using (ZipFile zip = ZipFile.Read(filename))
            {
                zippedGuid = zip.ElementAt(0).FileName;
                zip.ExtractAll(tempPath, ExtractExistingFileAction.OverwriteSilently);
            }

            // Find the zipped guid, we need it to know which files to process in the temp directory
            string[] guidArr = zippedGuid.Split('.');
            zippedGuid = guidArr[0];

            // Create a new guid for the imported note
            string newGuid = Guid.NewGuid().ToString();

            // Create a new flowdocument to hold the rtf of the unzipped note
            FlowDocument newFlowDoc = new FlowDocument();
            TextRange t = new TextRange(newFlowDoc.ContentStart, newFlowDoc.ContentEnd);
            FileStream f = new FileStream(System.IO.Path.Combine(tempPath, zippedGuid + ".xaml"), FileMode.Open);
            t.Load(f, System.Windows.DataFormats.XamlPackage);
            f.Close();

            // Save the contents of newFlowDoc to text
            TextRange tr = new TextRange(newFlowDoc.ContentStart, newFlowDoc.ContentEnd);
            string text = "";

            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, System.Windows.DataFormats.Text);
                text = tr.Text;
            }

            // Fetch the title of the zipped note
            XDocument doc = XDocument.Load(System.IO.Path.Combine(tempPath, zippedGuid + ".xml"));

            var noteTitle = (from s in doc.Element("Meta").Elements("Note")
                             select s.Attribute("Title").Value).FirstOrDefault().ToString();

            string importTitle = noteTitle + " (Imported)";

            // Make sure the new title is unique
            while (this.NoteExists(importTitle))
            {
                importTitle = importTitle + " (1)";
            }

            // Create a new note with the info from above
            this.NewNote(newFlowDoc, newGuid, importTitle, "");

            // Delete the temporary files
            System.IO.File.Delete(System.IO.Path.Combine(tempPath, zippedGuid + ".xaml"));
            System.IO.File.Delete(System.IO.Path.Combine(tempPath, zippedGuid + ".xml"));
        }
        #endregion
    }
}
