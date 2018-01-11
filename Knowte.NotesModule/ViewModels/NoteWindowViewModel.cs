using Knowte.Common.Services.Dialog;
using Prism.Commands;
using Prism.Mvvm;

namespace Knowte.NotesModule.ViewModels
{
    public class NoteWindowViewModel : BindableBase
    {
        private IDialogService dialogService;
        private bool isDimmed;
     
        public DelegateCommand ClosingCommand { get; set; }
      
        public bool IsDimmed
        {
            get { return this.isDimmed; }
            set { SetProperty<bool>(ref this.isDimmed, value); }
        }
   
        public NoteWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            this.ClosingCommand = new DelegateCommand(() => this.Cleanup());

            // Events
            this.dialogService.DialogVisibleChanged += DialogService_DialogVisibleChanged;
        }
      
        private void DialogService_DialogVisibleChanged(bool isDialogVisible)
        {
            this.IsDimmed = isDialogVisible;
        }

        private void Cleanup()
        {
            this.dialogService.DialogVisibleChanged -= DialogService_DialogVisibleChanged;
        }
    }
}
