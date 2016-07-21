using Knowte.Core.Database.Entities;
using Prism.Mvvm;
using System;

namespace Knowte.NotesModule.ViewModels
{
    public class NotebookViewModel : BindableBase
    {
        #region Variables
        private Notebook notebook;
        private string id;
        private string title;
        private DateTime creationDate;
        private string fontWeight;
        private bool isDragOver;
        #endregion

        #region Properties
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
        #endregion

        #region Overrides
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
        #endregion
    }
}