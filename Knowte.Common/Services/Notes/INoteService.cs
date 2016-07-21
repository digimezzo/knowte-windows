using Knowte.Core.Database.Entities;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace Knowte.Common.Services.Notes
{
    public interface INoteService
    {
        int GetNewNoteCount();
        void IncreaseNewNoteCount();
        void NewNote(FlowDocument document, string id, string title, string notebookId);
        void UpdateOpenDate(string id);
        void UpdateNoteParameters(string id, double width, double height, double top, double left, bool maximized);
        void UpdateNoteFlag(string id, bool flagged);
        void UpdateNote(FlowDocument document, string id, string title, string notebookId, double width, double height, double top, double left, bool maximized);
        void LoadNote(FlowDocument document, Note note);
        void DeleteNote(string id);
        Note GetNote(string title);
        Note GetNoteById(string id);
        bool NoteExists(string title);
        bool NoteIdExists(string id);
        List<Note> GetRecentlyOpenedNotes(int number);
        List<Note> GetFlaggedNotes();
        List<Note> GetNotes(Notebook notebook, string searchString, ref int count, bool orderByLastChanged, string noteFilter);
        void CountNotes(ref int allNotesCount, ref int todayNotesCount, ref int yesterdayNotesCount, ref int thisWeekNotesCount, ref int flaggedNotesCount);
        void ExportToRtf(string id, string title, string fileName);
        void Print(string id, string title);
        FlowDocument MergeDocument(string id, string title);
        void ExportFile(string noteId, string fileName);
        void ImportFile(string filename);
        event EventHandler FlagUpdated;
    }
}