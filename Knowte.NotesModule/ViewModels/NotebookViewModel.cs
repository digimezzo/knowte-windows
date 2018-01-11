using Digimezzo.Utilities.Utils;
using Knowte.Common.Database.Entities;
using Prism.Mvvm;
using System;

namespace Knowte.NotesModule.ViewModels
{
    public class NotebookViewModel : BindableBase
    {
        private Notebook notebook;
        private string id;
        private string title;
        private DateTime creationDate;
        private string fontWeight;
        private bool isDragOver;
    
        public Notebook Notebook
        {
            get { return this.notebook; }

            set
            {
                SetProperty<Notebook>(ref this.notebook, value);
                OnPropertyChanged(() => this.Id);
                OnPropertyChanged(() => this.Title);
                OnPropertyChanged(() => this.CreationDate);
                OnPropertyChanged(() => this.IsDefaultNotebook);
            }
        }

        public string Id
        {
            get { return this.Notebook.Id; }
        }

        public string Title
        {
            get { return this.Notebook.Title; }
            set
            {
                this.Notebook.Title = value;
                OnPropertyChanged(() => this.Title);
            }
        }

        public DateTime CreationDate
        {
            get { return new DateTime(this.Notebook.CreationDate); }
        }

        public bool IsDefaultNotebook
        {
            get { return this.Notebook.IsDefaultNotebook; }
        }

        public string FontWeight
        {
            get { return this.fontWeight; }
            set { SetProperty<string>(ref this.fontWeight, value); }
        }

        public bool IsDragOver
        {
            get { return this.isDragOver; }
            set { SetProperty<bool>(ref this.isDragOver, value); }
        }
   
        public static NotebookViewModel CreateAllNotesNotebook()
        {
            return new NotebookViewModel()
            {
                Notebook = new Notebook
                {
                    Title = ResourceUtils.GetString("Language_All_Notes"),
                    Id = "0",
                    CreationDate = DateTime.Now.Ticks,
                    IsDefaultNotebook = true
                },
                FontWeight = "Bold",
                IsDragOver = false
            };
        }

        public static NotebookViewModel CreateUnfiledNotesNotebook()
        {
            return new NotebookViewModel()
            {
                Notebook = new Notebook
                {
                    Title = ResourceUtils.GetString("Language_Unfiled_Notes"),
                    Id = "1",
                    CreationDate = DateTime.Now.Ticks,
                    IsDefaultNotebook = true
                },
                FontWeight = "Bold",
                IsDragOver = false
            };
        }
     
        public override string ToString()
        {
            return this.Title;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return this.Id.Equals(((NotebookViewModel)obj).Id);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}