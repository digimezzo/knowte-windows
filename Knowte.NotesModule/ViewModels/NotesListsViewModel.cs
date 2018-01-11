using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Database.Entities;
using Knowte.Common.IO;
using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.Backup;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.I18n;
using Knowte.Common.Services.Note;
using Knowte.Common.Services.Search;
using Knowte.Common.Services.WindowsIntegration;
using Knowte.Common.Utils;
using Knowte.NotesModule.Views;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using WPFFolderBrowser;

namespace Knowte.NotesModule.ViewModels
{
    public class NotesListsViewModel : BindableBase
    {
        private ObservableCollection<NotebookViewModel> notebooks;
        private ObservableCollection<NoteViewModel> notes;
        private NotebookViewModel selectedNotebook;
        private NoteViewModel selectedNote;
        private int total;
        private double mainWidth;
        private double mainHeight;

        private int totalNotebooks;
        private string noteFilter;

        // Delegate with default event handler signature
        public delegate void NotebooksChangedDelegate(object sender, EventArgs e);
        public delegate void ThemeChangedDelegate(object sender, EventArgs e);

        private IEventAggregator eventAggregator;
        private INoteService noteService;
        private IAppearanceService appearanceService;
        private IJumpListService jumpListService;
        private ISearchService searchService;
        private IDialogService dialogService;
        private I18nService i18nService;
        private IBackupService backupService;
        private bool triggerRefreshNotesAnimation;
     
        public DelegateCommand<string> NewNotebookCommand { get; set; }
        public DelegateCommand<object> NewNoteCommand { get; set; }
        public DelegateCommand<string> ImportNoteCommand { get; set; }
        public DelegateCommand<object> NavigateBetweenNotesCommand { get; set; }
        public DelegateCommand<object> DeleteNoteCommand { get; set; }
        public DelegateCommand<object> ToggleNoteFlagCommand { get; set; }
        public DelegateCommand<object> DeleteNotebookCommand { get; set; }
        public DelegateCommand<object> EditNotebookCommand { get; set; }
        public DelegateCommand EditSelectedNotebookCommand { get; set; }
        public DelegateCommand DeleteSelectedNotebookCommand { get; set; }
        public DelegateCommand DeleteSelectedNoteCommand { get; set; }
        public DelegateCommand ChangeStorageLocationCommand { get; set; }
        public DelegateCommand ResetStorageLocationCommand { get; set; }
  
        public bool ShowChangeStorageLocationButton
        {
            get { return SettingsClient.Get<bool>("Advanced", "ChangeStorageLocationFromMain"); }
        }

        public bool TriggerRefreshNotesAnimation
        {
            get { return triggerRefreshNotesAnimation; }
            set { SetProperty<bool>(ref this.triggerRefreshNotesAnimation, value); }
        }

        public string NoteFilter
        {
            get { return this.noteFilter; }
            set { SetProperty<string>(ref this.noteFilter, value); }
        }

        public ObservableCollection<NotebookViewModel> Notebooks
        {
            get { return this.notebooks; }

            set
            {
                // SetProperty doesn't notify when the elements of the collection change.
                // So we have to do an old school OnPropertyChanged
                this.notebooks = value;
                OnPropertyChanged(() => this.Notebooks);
            }
        }

        public ObservableCollection<NoteViewModel> Notes
        {
            get { return this.notes; }
            set { SetProperty<ObservableCollection<NoteViewModel>>(ref this.notes, value); }
        }

        public NotebookViewModel SelectedNotebook
        {
            get { return this.selectedNotebook; }

            set
            {
                SetProperty<NotebookViewModel>(ref this.selectedNotebook, value);

                try
                {
                    this.RefreshNotesAnimated();

                }
                catch (Exception)
                {
                }
            }
        }

        public NoteViewModel SelectedNote
        {
            get { return this.selectedNote; }
            set { SetProperty<NoteViewModel>(ref this.selectedNote, value); }
        }

        public int Total
        {
            get { return this.total; }
            set { SetProperty<int>(ref this.total, value); }
        }

        public int TotalNotebooks
        {
            get { return this.totalNotebooks; }
            set { SetProperty<int>(ref this.totalNotebooks, value); }
        }

        public double MainWidth
        {
            get { return this.mainWidth; }
            set { SetProperty<double>(ref this.mainWidth, value); }
        }

        public double MainHeight
        {
            get { return this.mainHeight; }
            set { SetProperty<double>(ref this.mainHeight, value); }
        }
     
        public NotesListsViewModel(IEventAggregator eventAggregator, INoteService noteService, IAppearanceService appearanceService, IJumpListService jumpListService, ISearchService searchService, IDialogService dialogService, I18nService i18nService, IBackupService backupService)
        {
            // Injection
            this.eventAggregator = eventAggregator;
            this.noteService = noteService;
            this.appearanceService = appearanceService;
            this.jumpListService = jumpListService;
            this.searchService = searchService;
            this.dialogService = dialogService;
            this.i18nService = i18nService;
            this.backupService = backupService;

            // PubSub events
            this.eventAggregator.GetEvent<TriggerLoadNoteAnimationEvent>().Subscribe((_) =>
            {
                this.TriggerRefreshNotesAnimation = false;
                this.TriggerRefreshNotesAnimation = true;
            });

            this.eventAggregator.GetEvent<SettingChangeStorageLocationFromMainChangedEvent>().Subscribe((_) => OnPropertyChanged(() => this.ShowChangeStorageLocationButton));
            this.eventAggregator.GetEvent<RefreshJumpListEvent>().Subscribe((_) => this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes()));

            this.eventAggregator.GetEvent<OpenNoteEvent>().Subscribe(noteTitle =>
            {
                if (!string.IsNullOrEmpty(noteTitle)) this.SelectedNote = new NoteViewModel { Title = noteTitle };
                this.OpenSelectedNote();
            });

            // Event handlers
            this.i18nService.LanguageChanged += LanguageChangedHandler;
            this.noteService.FlagUpdated += async (noteId, isFlagged) => { await this.UpdateNoteFlagAsync(noteId, isFlagged); };
            this.noteService.StorageLocationChanged += (_, __) => this.RefreshNotebooksAndNotes();
            this.backupService.BackupRestored += (_, __) => this.RefreshNotebooksAndNotes();
            this.noteService.NotesChanged += (_, __) => Application.Current.Dispatcher.Invoke(() => { this.RefreshNotes(); });
            this.searchService.Searching += (_, __) => TryRefreshNotesOnSearch();

            this.NoteFilter = ""; // Must be set before RefreshNotes()

            // Initialize notebooks
            this.RefreshNotebooksAndNotes();

            // Commands
            this.DeleteNoteCommand = new DelegateCommand<object>(async (obj) => await this.DeleteNoteAsync(obj));
            this.ToggleNoteFlagCommand = new DelegateCommand<object>((obj) => this.ToggleNoteFlag(obj));
            this.DeleteNotebookCommand = new DelegateCommand<object>((obj) => this.DeleteNotebook(obj));
            this.EditNotebookCommand = new DelegateCommand<object>((obj) => this.EditNotebook(obj));
            this.DeleteSelectedNotebookCommand = new DelegateCommand(() => this.DeleteSelectedNotebook());
            this.EditSelectedNotebookCommand = new DelegateCommand(() => this.EditSelectedNotebook());
            this.DeleteSelectedNoteCommand = new DelegateCommand(async () => await this.DeleteSelectedNoteAync());
            this.ChangeStorageLocationCommand = new DelegateCommand(async () => await this.ChangeStorageLocationAsync(false));
            this.ResetStorageLocationCommand = new DelegateCommand(async () => await this.ChangeStorageLocationAsync(true));

            this.NewNotebookCommand = new DelegateCommand<string>((_) => this.NewNotebook());
            Common.Prism.ApplicationCommands.NewNotebookCommand.RegisterCommand(this.NewNotebookCommand);

            this.NewNoteCommand = new DelegateCommand<object>(param => this.NewNote(param));
            Common.Prism.ApplicationCommands.NewNoteCommand.RegisterCommand(this.NewNoteCommand);

            this.ImportNoteCommand = new DelegateCommand<string>((_) => this.ImportNote());
            Common.Prism.ApplicationCommands.ImportNoteCommand.RegisterCommand(this.ImportNoteCommand);

            this.NavigateBetweenNotesCommand = new DelegateCommand<object>(NavigateBetweenNotes);
            Common.Prism.ApplicationCommands.NavigateBetweenNotesCommand.RegisterCommand(this.NavigateBetweenNotesCommand);

            // Process jumplist commands
            this.ProcessJumplistCommands();
        }
      
        private async Task ChangeStorageLocationAsync(bool performReset)
        {
            string selectedFolder = ApplicationPaths.CurrentNoteStorageLocation;

            if (performReset)
            {
                bool confirmPerformReset = this.dialogService.ShowConfirmationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Reset"),
                    content: ResourceUtils.GetString("Language_Reset_Confirm"),
                    okText: ResourceUtils.GetString("Language_Yes"),
                    cancelText: ResourceUtils.GetString("Language_No"));

                if (confirmPerformReset) selectedFolder = ApplicationPaths.DefaultNoteStorageLocation;
            }
            else
            {
                var dlg = new WPFFolderBrowserDialog();
                dlg.InitialDirectory = ApplicationPaths.CurrentNoteStorageLocation;
                if ((bool)dlg.ShowDialog()) selectedFolder = dlg.FileName;
            }

            // If the new folder is the same as the old folder, do nothing.
            if (ApplicationPaths.CurrentNoteStorageLocation.Equals(selectedFolder, StringComparison.InvariantCultureIgnoreCase)) return;

            bool isChangeStorageLocationSuccess = await this.noteService.ChangeStorageLocationAsync(selectedFolder, false);

            if (!isChangeStorageLocationSuccess)
            {
                // Show error if change storage location failed. Don't notify when changing the storage location was successful.
                // The user is on the main screen, and sees this immediately when the notebooks and notes get refreshed.
                this.dialogService.ShowNotificationDialog(
                  null,
                  title: ResourceUtils.GetString("Language_Error"),
                  content: ResourceUtils.GetString("Language_Error_Change_Storage_Location_Error"),
                  okText: ResourceUtils.GetString("Language_Ok"),
                  showViewLogs: true);
            }
        }

        private async Task DeleteSelectedNoteAync()
        {
            if (this.SelectedNote == null) return;

            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetString("Language_Delete_Note"), content: ResourceUtils.GetString("Language_Delete_Note_Confirm").Replace("%notename%", this.SelectedNote.Title), okText: ResourceUtils.GetString("Language_Yes"), cancelText: ResourceUtils.GetString("Language_No"));

            if (dialogResult)
            {
                await this.noteService.DeleteNoteAsync(this.SelectedNote.Id);
                this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
            }
        }

        private void EditSelectedNotebook()
        {
            if (this.selectedNotebook == null) return;

            string responseText = this.selectedNotebook.Title;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetString("Language_Edit_Notebook"), content: ResourceUtils.GetString("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", this.selectedNotebook.Title), okText: ResourceUtils.GetString("Language_Ok"), cancelText: ResourceUtils.GetString("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.noteService.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.noteService.UpdateNotebook(this.SelectedNotebook.Title, responseText);

                        this.RefreshNotebooksAndNotes();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                }
            }
        }
        private void DeleteSelectedNotebook()
        {
            if (this.selectedNotebook == null) return;

            this.ConfirmDeleteNotebook(this.SelectedNotebook.Notebook);
        }
        private void EditNotebook(object obj)
        {
            Notebook notebook = this.noteService.GetNotebook(obj as string);

            string responseText = notebook.Title;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetString("Language_Edit_Notebook"), content: ResourceUtils.GetString("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetString("Language_Ok"), cancelText: ResourceUtils.GetString("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.noteService.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.noteService.UpdateNotebook(notebook.Id, responseText);

                        this.RefreshNotebooksAndNotes();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                }
            }
        }

        private void ConfirmDeleteNotebook(Notebook notebook)
        {
            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetString("Language_Delete_Notebook"), content: ResourceUtils.GetString("Language_Delete_Notebook_Confirm").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetString("Language_Yes"), cancelText: ResourceUtils.GetString("Language_No"));

            if (dialogResult)
            {
                this.noteService.DeleteNotebook(notebook.Id);
                this.RefreshNotebooksAndNotes();
            }
        }

        private void DeleteNotebook(object obj)
        {
            this.ConfirmDeleteNotebook(this.noteService.GetNotebook(obj as string));
        }

        private async Task DeleteNoteAsync(object obj)
        {
            if (obj != null)
            {
                Note theNote = this.noteService.GetNote(obj as string);

                bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetString("Language_Delete_Note"), content: ResourceUtils.GetString("Language_Delete_Note_Confirm").Replace("%notename%", theNote.Title), okText: ResourceUtils.GetString("Language_Yes"), cancelText: ResourceUtils.GetString("Language_No"));


                if (dialogResult)
                {
                    await this.noteService.DeleteNoteAsync(theNote.Id);
                    this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());

                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win.Title == theNote.Title)
                        {
                            win.Close();
                        }
                    }
                }
            }
        }

        private void TryRefreshNotesOnSearch()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => { this.RefreshNotes(); });
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not refresh after search. Exception: {0}", ex.Message);
            }
        }

        private void ProcessJumplistCommands()
        {
            try
            {
                if (this.jumpListService.NewNoteFromJumplist)
                {
                    this.NewNote(true);
                }

                if (this.jumpListService.OpenNoteFromJumplist)
                {
                    this.SelectedNote = new NoteViewModel { Title = this.jumpListService.OpenNoteFromJumplistTitle };
                    this.OpenSelectedNote();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                this.jumpListService.NewNoteFromJumplist = false;
                this.jumpListService.OpenNoteFromJumplist = false;
                this.jumpListService.OpenNoteFromJumplistTitle = "";
            }
        }

        private void OpenSelectedNote()
        {
            if (this.SelectedNote == null)
            {
                return;
            }

            try
            {
                foreach (Window existingWindow in Application.Current.Windows)
                {
                    if (!existingWindow.Equals(Application.Current.MainWindow) && existingWindow.Title == this.SelectedNote.Title)
                    {
                        existingWindow.WindowState = WindowState.Normal;
                        // In case the window is minimized

                        ((NoteWindow)existingWindow).ActivateNow();
                        return;
                    }
                }

                NoteWindow newWindow = new NoteWindow(this.SelectedNote.Title, "", this.noteService.GetNotebook(this.SelectedNote.NotebookId), this.searchService.SearchText, false, this.appearanceService, this.jumpListService, this.eventAggregator, this.noteService,
                this.dialogService);

                ((NoteWindow)newWindow).ActivateNow();
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not open the note. Exception: {0}", ex.Message);
            }
        }

        private void ImportNote()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = ProductInformation.ApplicationName + " file (*." + Defaults.ExportFileExtension + ")|*." + Defaults.ExportFileExtension + "|All files (*.*)|*.*";

            string lastExportDirectory = SettingsClient.Get<string>("General", "LastExportDirectory");

            if (!string.IsNullOrEmpty(lastExportDirectory))
            {
                dlg.InitialDirectory = lastExportDirectory;
            }
            else
            {
                // If no LastRtfDirectory is set, default to the My Documents folder
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if ((bool)dlg.ShowDialog())
            {
                string importFile = dlg.FileName;

                try
                {
                    if (!MiscUtils.IsValidExportFile(importFile))
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Invalid_File"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                    }
                    else
                    {
                        // Try to import
                        this.noteService.ImportFile(importFile);
                        this.RefreshNotes();
                    }

                }
                catch (UnauthorizedAccessException)
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                }
            }
        }

        private void NewNote(object param)
        {
            bool createUnfiled = Convert.ToBoolean(param);

            try
            {
                string initialTitle = this.noteService.GetUniqueNoteTitle(ResourceUtils.GetString("Language_New_Note"));

                FlowDocument flowDoc = new FlowDocument();

                Notebook theNotebook = default(Notebook);

                if (!createUnfiled & (this.SelectedNotebook != null && !this.SelectedNotebook.IsDefaultNotebook))
                {
                    theNotebook = this.SelectedNotebook.Notebook;
                }
                else
                {
                    theNotebook = NotebookViewModel.CreateUnfiledNotesNotebook().Notebook;
                }

                NoteWindow notewin = new NoteWindow(initialTitle, "", theNotebook, this.searchService.SearchText, true, this.appearanceService, this.jumpListService, this.eventAggregator, this.noteService,
                this.dialogService);

                notewin.ActivateNow();

                RefreshNotes();
            }
            catch (Exception)
            {
                this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Problem_Creating_Note"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
            }
        }

        private void NewNotebook()
        {
            this.selectedNotebook = null;

            string responseText = string.Empty;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetString("Language_New_Notebook"), content: ResourceUtils.GetString("Language_New_Notebook_Enter_Name"), okText: ResourceUtils.GetString("Language_Ok"), cancelText: ResourceUtils.GetString("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {
                if (!string.IsNullOrEmpty(responseText))
                {
                    Notebook newNotebook = new Notebook
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = responseText,
                        CreationDate = DateTime.Now.Ticks,
                        IsDefaultNotebook = false
                    };

                    try
                    {

                        if (!this.noteService.NotebookExists(newNotebook))
                        {
                            this.noteService.NewNotebook(newNotebook);

                            this.RefreshNotebooksAndNotes();
                            this.SelectedNotebook = new NotebookViewModel
                            {
                                Notebook = new Notebook
                                {
                                    Id = newNotebook.Id,
                                    Title = newNotebook.Title,
                                    CreationDate = DateTime.Now.Ticks
                                }
                            };
                        }
                        else
                        {
                            this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                        }
                    }
                    catch (Exception)
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Problem_Creating_Notebook"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetString("Language_Error"), content: ResourceUtils.GetString("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetString("Language_Ok"), showViewLogs: false);
                }
            }
        }

        private async Task UpdateNoteFlagAsync(string noteId, bool isFlagged)
        {
            if (this.NoteFilter.Equals("Flagged"))
            {
                // If we're looking at flagged notes, we need a complete refresh of the list.
                this.RefreshNotes();
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (NoteViewModel note in this.Notes)
                    {
                        if (note.Id.Equals(noteId)) note.Flagged = isFlagged;
                    }
                });
            }
        }

        private void LanguageChangedHandler(object sender, EventArgs e)
        {
            foreach (NotebookViewModel nb in this.Notebooks)
            {
                if (nb.Id == "0")
                    nb.Title = ResourceUtils.GetString("Language_All_Notes");
                if (nb.Id == "1")
                    nb.Title = ResourceUtils.GetString("Language_Unfiled_Notes");
            }

            if (this.SelectedNotebook != null)
            {
                if (this.SelectedNotebook.Id == "0")
                    this.SelectedNotebook.Title = ResourceUtils.GetString("Language_All_Notes");
                if (this.SelectedNotebook.Id == "1")
                    this.SelectedNotebook.Title = ResourceUtils.GetString("Language_Unfiled_Notes");

                OnPropertyChanged(() => this.SelectedNotebook);
            }
        }

        private void NavigateBetweenNotes(object iNoteFilter)
        {

            if (iNoteFilter != null)
            {
                this.NoteFilter = Convert.ToString(iNoteFilter);
                this.RefreshNotesAnimated();
            }
        }

        private void SetDefaultSelectedNotebook()
        {
            try
            {
                this.SelectedNotebook = NotebookViewModel.CreateAllNotesNotebook();
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not set selected notebook. Exception: {0}", ex.Message);
            }
        }

        private void RefreshNotesAnimated()
        {
            this.RefreshNotes();
            this.eventAggregator.GetEvent<TriggerLoadNoteAnimationEvent>().Publish("");
        }

        private void ToggleNoteFlag(object obj)
        {
            if (obj != null)
            {
                Note theNote = this.noteService.GetNote(obj as string);

                if (theNote.Flagged == 1)
                {
                    theNote.Flagged = 0;
                    this.noteService.UpdateNoteFlag(theNote.Id, false);
                }
                else
                {
                    theNote.Flagged = 1;
                    this.noteService.UpdateNoteFlag(theNote.Id, true);
                }

                this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
            }
        }

        private void RefreshNotes()
        {
            this.Notes = new ObservableCollection<NoteViewModel>();

            string formatDate = "D";
            string text = "";

            if (this.searchService.SearchText != null)
            {
                text = this.searchService.SearchText.Trim();
            }

            this.Notes.Clear();

            NotebookViewModel selectedNotebook = this.SelectedNotebook != null ? this.SelectedNotebook : NotebookViewModel.CreateAllNotesNotebook();
            var notes = this.noteService.GetNotes(selectedNotebook.Notebook, text, ref this.total, SettingsClient.Get<bool>("Appearance", "SortByModificationDate"), this.NoteFilter);

            foreach (Note note in notes)
            {
                this.Notes.Add(new NoteViewModel()
                {
                    Title = note.Title,
                    Id = note.Id,
                    NotebookId = note.NotebookId,
                    OpenDate = new DateTime(note.OpenDate),
                    OpenDateText = DateUtils.DateDifference(new DateTime(note.OpenDate), DateTime.Now, formatDate, false),
                    ModificationDate = new DateTime(note.ModificationDate),
                    Flagged = note.Flagged == 1 ? true : false,
                    ModificationDateText = DateUtils.DateDifference(new DateTime(note.ModificationDate), DateTime.Now, formatDate, false),
                    ModificationDateTextSimple = DateUtils.DateDifference(new DateTime(note.ModificationDate), DateTime.Now, formatDate, true)
                });
            }

            // Because we cannot pass Property "Total" by reference, we need to force a refresh.
            OnPropertyChanged<int>(() => this.Total);

            this.eventAggregator.GetEvent<CountNotesEvent>().Publish("");
        }

        private void RefreshNotebooksAndNotes()
        {
            this.Notebooks = null;
            var localNotebooks = new ObservableCollection<NotebookViewModel>();

            // Add the default notebooks
            localNotebooks.Add(NotebookViewModel.CreateAllNotesNotebook());
            localNotebooks.Add(NotebookViewModel.CreateUnfiledNotesNotebook());

            foreach (Notebook nb in this.noteService.GetNotebooks(ref this.totalNotebooks))
            {
                localNotebooks.Add(new NotebookViewModel()
                {
                    Notebook = nb,
                    FontWeight = "Normal",
                    IsDragOver = false
                });
            }

            this.Notebooks = localNotebooks;

            // Because we cannot pass a Property by reference aboves
            OnPropertyChanged(() => this.TotalNotebooks);

            // Set the default selected notebook (Setting the selected notebook, triggers a refresh of the notes.)
            this.SetDefaultSelectedNotebook();

            // This makes sure the View is notified that the Notebooks collection has changed. 
            // If this call is missing, the list of Notebooks is not updated in the View after 
            // we changed its elements here. OnPropertyChanged("Notebooks")
            this.eventAggregator.GetEvent<NotebooksChangedEvent>().Publish("");
        }
    }
}