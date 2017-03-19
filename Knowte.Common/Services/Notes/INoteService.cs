using Knowte.Common.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace Knowte.Common.Services.Note
{
    public delegate void FlagUpdatedEventHandler(string noteId, bool isFlagged);

    public interface INoteService
    {
        string GetUniqueNoteTitle(string proposedTitle);
        Task MigrateAsync(string sourceFolder, bool deleteDestination);
        Task<bool> ChangeStorageLocationAsync(string newStorageLocation, bool moveCurrentNotes);
        Task CloseAllNoteWindowsAsync(int delayMilliseconds = 0);
        void NewNotebook(Notebook notebook);
        void DeleteNotebook(string id);
        void UpdateNotebook(string id, string newTitle);
        bool NotebookExists(Notebook notebook);
        string GetNotebookId(string notebookTitle);
        List<Notebook> GetNotebooks(ref int totalNotebooks);
        List<Notebook> GetNotebooks();
        Notebook GetNotebook(string id);
        void NewNote(FlowDocument document, string id, string title, string notebookId);
        void UpdateOpenDate(string id);
        void UpdateNoteParameters(string id, double width, double height, double top, double left, bool maximized);
        void UpdateNoteFlag(string id, bool flagged);
        void UpdateNote(FlowDocument document, string id, string title, string notebookId, double width, double height, double top, double left, bool maximized);
        LoadNoteResult LoadNote(FlowDocument document, Database.Entities.Note note);
        Task DeleteNoteAsync(string id);
        Database.Entities.Note GetNote(string title);
        Database.Entities.Note GetNoteById(string id);
        bool NoteExists(string title);
        bool NoteIdExists(string id);
        List<Database.Entities.Note> GetRecentlyOpenedNotes(int number);
        List<Database.Entities.Note> GetFlaggedNotes();
        List<Database.Entities.Note> GetNotes(Notebook notebook, string searchString, ref int count, bool orderByLastChanged, string noteFilter);
        void CountNotes(ref int allNotesCount, ref int todayNotesCount, ref int yesterdayNotesCount, ref int thisWeekNotesCount, ref int flaggedNotesCount);
        void ExportToRtf(string id, string title, string fileName);
        void Print(string id, string title);
        FlowDocument MergeDocument(string id, string title);
        void ExportFile(string noteId, string fileName);
        void ImportFile(string filename);
        event FlagUpdatedEventHandler FlagUpdated;
        event EventHandler StorageLocationChanged;
        event EventHandler NotesChanged;
        void OnNotesChanged();
    }
}