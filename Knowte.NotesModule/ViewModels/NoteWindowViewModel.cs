using Knowte.Common.Services.Dialog;
using Prism.Mvvm;

namespace Knowte.NotesModule.ViewModels
{
    public class NoteWindowViewModel : BindableBase
    {
        #region Variables
        private IDialogService dialogService;
        private bool isDimmed;
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

            // Events
            this.dialogService.DialogVisibleChanged += isDialogVisible => this.IsDimmed = isDialogVisible;
        }
        #endregion
    }
}
