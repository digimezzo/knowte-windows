using System;
using Knowte.Common.Services.Dialog;
using Prism.Commands;
using Prism.Mvvm;

namespace Knowte.NotesModule.ViewModels
{
    public class NoteWindowViewModel : BindableBase
    {
        #region Variables
        private IDialogService dialogService;
        private bool isDimmed;
        #endregion

        #region Commands
        public DelegateCommand ClosingCommand { get; set; }
        #endregion

        #region Properties
        public bool IsDimmed
        {
            get { return this.isDimmed; }
            set { SetProperty<bool>(ref this.isDimmed, value); }
        }
        #endregion

        #region Construction
        public NoteWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            this.ClosingCommand = new DelegateCommand(() => this.Cleanup());

            // Events
            this.dialogService.DialogVisibleChanged += DialogService_DialogVisibleChanged;
        }
        #endregion

        #region Private
        private void DialogService_DialogVisibleChanged(bool isDialogVisible)
        {
            this.IsDimmed = isDialogVisible;
        }

        private void Cleanup()
        {
            this.dialogService.DialogVisibleChanged -= DialogService_DialogVisibleChanged;
        }
        #endregion
    }
}
