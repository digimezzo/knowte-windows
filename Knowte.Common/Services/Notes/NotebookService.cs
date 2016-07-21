using Knowte.Core.Base;
using Knowte.Core.Database;
using Knowte.Core.Database.Entities;
using Knowte.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Knowte.Common.Services.Notes
{
    public class NotebookService : INotebookService
    {
        #region Variables
        private string applicationFolder = XmlSettingsClient.Instance.ApplicationFolder;
        private SQLiteConnectionFactory factory;
        #endregion

        #region Construction
        public NotebookService()
        {
            this.factory = new SQLiteConnectionFactory();
        }
        #endregion

        #region INotebookService
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
                List<Note> notesToUpdate = conn.Table<Note>().Where((n) => n.NotebookId == id).ToList();

                if (notesToUpdate != null & notesToUpdate.Count > 0)
                {
                    foreach (Note noteToUpdate in notesToUpdate)
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
                //notebooks = conn.Table<Notebook>().ToList();
                //notebooks = notebooks.OrderBy((nb) => nb.Title.ToLower()).ToList();
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

                if(notebookToUpdate != null)
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
                notebookId = conn.Table<Notebook>().Where((nb) => nb.Title == notebookTitle).Select((nb) => nb.Id).FirstOrDefault();
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

            if(requestedNotebook == null)
            {
                requestedNotebook = new Notebook { Id = "1", Title = "Unfiled notes", CreationDate = DateTime.Now.Ticks, IsDefaultNotebook = true };
            }

            return requestedNotebook;
        }
        #endregion
    }
}
