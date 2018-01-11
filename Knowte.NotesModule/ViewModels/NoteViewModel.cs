using Prism.Mvvm;
using System;

namespace Knowte.NotesModule.ViewModels
{
    public class NoteViewModel : BindableBase
    {
        private string title;
        private string id;
        private string notebookId;
        private DateTime openDate;
        private string openDateText;
        private DateTime modificationDate;
        private string modificationDateText;
        private string modificationDateTextSimple;
        private bool flagged;
    
        public string Title
        {
            get { return this.title; }
            set { SetProperty<string>(ref this.title, value); }
        }

        public string Id
        {
            get { return this.id; }
            set { SetProperty<string>(ref this.id, value); }
        }

        public string NotebookId
        {
            get { return this.notebookId; }
            set { SetProperty<string>(ref this.notebookId, value); }
        }

        public DateTime OpenDate
        {
            get { return this.openDate; }
            set
            {
                this.openDate = value;
                SetProperty<DateTime>(ref this.openDate, value);
            }
        }

        public string OpenDateText
        {
            get { return this.openDateText; }
            set { SetProperty<string>(ref this.openDateText, value); }
        }

        public DateTime ModificationDate
        {
            get { return this.modificationDate; }
            set { SetProperty<DateTime>(ref this.modificationDate, value); }
        }

        public string ModificationDateText
        {
            get { return this.modificationDateText; }
            set { SetProperty<string>(ref this.modificationDateText, "Last changed: " + value); }
        }

        public string ModificationDateTextSimple
        {
            get { return this.modificationDateTextSimple; }
            set { SetProperty<string>(ref this.modificationDateTextSimple, value); }
        }

        public bool Flagged
        {
            get { return this.flagged; }
            set { SetProperty<bool>(ref this.flagged, value); }
        }
    }
}
