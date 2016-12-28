using Knowte.Common.Database.Entities;
using System.Collections.Generic;

namespace Knowte.Common.Services.Notes
{
    public interface INotebookService
    {
        void NewNotebook(Notebook notebook);
        void DeleteNotebook(string id);
        void UpdateNotebook(string id, string newTitle);
        bool NotebookExists(Notebook notebook);
        string GetNotebookId(string notebookTitle);
        List<Notebook> GetNotebooks(ref int totalNotebooks);
        List<Notebook> GetNotebooks();
        Notebook GetNotebook(string id);
    }
}