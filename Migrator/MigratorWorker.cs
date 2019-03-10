using Digimezzo.Utilities.IO;
using Knowte.Common.Base;
using Knowte.Common.Database;
using Knowte.Common.Database.Entities;
using Knowte.Common.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Migrator
{
    public class MigratorWorker
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

        public MigratorWorker()
        {
        }

        public void Execute()
        {
            Console.WriteLine("Migrator");
            Console.WriteLine("========");

            // Workaround so that the Migrator is able to find the Knowte database
            string storageLocation = ApplicationPaths.CurrentNoteStorageLocation.Replace("Migrator", "Knowte");

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
            List<Note> notes = null;

            using (var conn = factory.GetConnection())
            {
                notes = conn.Query<Note>("SELECT * FROM Note;");
            }

            // Export notes
            var notesJson = new List<NoteJson>();

            foreach (Note note in notes)
            {
                Notebook notebook = notebooks.Where(x => x.Id.Equals(note.NotebookId)).FirstOrDefault();
                string notebookTitle = string.Empty;

                if(notebook != null)
                {
                    notebookTitle = notebook.Title;
                }

                var noteJson = new NoteJson();
                noteJson.Title = note.Title;
                noteJson.Text = note.Text;
                noteJson.Notebook = notebookTitle;
                noteJson.CreationDate = new DateTime(note.CreationDate).ToString("yyyy-MM-dd hh:mm:ss");
                noteJson.ModificationDate = new DateTime(note.ModificationDate).ToString("yyyy-MM-dd hh:mm:ss");
                notesJson.Add(noteJson);
            }

            string notesJsonString = JsonConvert.SerializeObject(notesJson);

            Console.WriteLine(Environment.NewLine + "Press any key to close this window...");
            Console.ReadKey();
        }
    }
}
