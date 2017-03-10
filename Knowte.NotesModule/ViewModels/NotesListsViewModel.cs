using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Database.Entities;
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
using System.Windows.Input;

namespace Knowte.NotesModule.ViewModels
{
    public class NotesListsViewModel : BindableBase
    {
        #region Variables
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
        #endregion

        #region Commands
        public DelegateCommand<string> NewNotebookCommand { get; set; }
        public DelegateCommand<object> NewNoteCommand { get; set; }
        public DelegateCommand<string> ImportNoteCommand { get; set; }
        public DelegateCommand<string> OpenNoteCommand { get; set; }
        public DelegateCommand<object> NavigateBetweenNotesCommand { get; set; }
        public DelegateCommand<object> DeleteNoteFromListCommand { get; set; }
        #endregion

        #region Properties
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
                //SetProperty(Of ObservableCollection(Of NotebookViewModel))(mNotebooks, value)

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
        #endregion

        #region Construction
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

            this.eventAggregator.GetEvent<RefreshJumpListEvent>().Subscribe((_) => this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes()));
            this.eventAggregator.GetEvent<NewNoteEvent>().Subscribe(createUnfiled => this.NewNoteCommand.Execute(createUnfiled));
            this.eventAggregator.GetEvent<DeleteNoteEvent>().Subscribe((_) => this.DeleteNote.Execute(null));
            this.eventAggregator.GetEvent<DeleteNotebookEvent>().Subscribe((_) => this.DeleteNotebook.Execute(null));
            this.eventAggregator.GetEvent<RefreshNotesEvent>().Subscribe((_) => this.RefreshNotes());

            this.eventAggregator.GetEvent<OpenNoteEvent>().Subscribe(noteTitle =>
            {
                if (!string.IsNullOrEmpty(noteTitle)) this.SelectedNote = new NoteViewModel { Title = noteTitle };
                this.OpenNoteCommand.Execute(null);
            });

            // Event handlers
            this.i18nService.LanguageChanged += LanguageChangedHandler;
            this.noteService.FlagUpdated += async (noteId, isFlagged) => { await this.UpdateNoteFlagAsync(noteId, isFlagged); };
            this.noteService.StorageLocationChanged += RefreshAllHandler;
            this.backupService.BackupRestored += RefreshAllHandler;
            this.searchService.Searching += (_, __) => TryRefreshNotesOnSearch();

            // Initialize notebooks
            this.RefreshNotebooks();
            this.SetDefaultSelectedNotebook();

            // Initialize notes
            this.NoteFilter = ""; // Must be set before RefreshNotes()
            this.RefreshNotes();

            // Commands
            this.DeleteNoteFromListCommand = new DelegateCommand<object>((obj) => this.DeleteNoteFromList(obj));

            this.NewNotebookCommand = new DelegateCommand<string>((_) => this.NewNotebook());
            Common.Prism.ApplicationCommands.NewNotebookCommand.RegisterCommand(this.NewNotebookCommand);

            this.NewNoteCommand = new DelegateCommand<object>(param => this.NewNote(param));
            Common.Prism.ApplicationCommands.NewNoteCommand.RegisterCommand(this.NewNoteCommand);

            this.ImportNoteCommand = new DelegateCommand<string>((x) => this.ImportNote());
            Common.Prism.ApplicationCommands.ImportNoteCommand.RegisterCommand(this.ImportNoteCommand);

            this.NavigateBetweenNotesCommand = new DelegateCommand<object>(NavigateBetweenNotes);
            Common.Prism.ApplicationCommands.NavigateBetweenNotesCommand.RegisterCommand(this.NavigateBetweenNotesCommand);

            this.OpenNoteCommand = new DelegateCommand<string>((_) => this.OpenNote());

            // Process jumplist commands
            this.ProcessJumplistCommands();
        }
        #endregion

        #region Private
        private void DeleteNoteFromList(object obj)
        {
            if (obj != null)
            {
                Note theNote = this.noteService.GetNote(obj as string);

                bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetStringResource("Language_Delete_Note"), content: ResourceUtils.GetStringResource("Language_Delete_Note_Confirm").Replace("%notename%", theNote.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));


                if (dialogResult)
                {
                    this.noteService.DeleteNote(theNote.Id);
                    this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());

                    try
                    {
                        this.RefreshNotes();
                    }
                    catch (Exception)
                    {
                    }

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
                    this.NewNoteCommand.Execute(null);
                }

                if (this.jumpListService.OpenNoteFromJumplist)
                {
                    this.SelectedNote = new NoteViewModel { Title = this.jumpListService.OpenNoteFromJumplistTitle };
                    this.OpenNoteCommand.Execute(null);
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

        private void OpenNote()
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
            dlg.Filter = ProductInformation.ApplicationDisplayName + " file (*." + Defaults.ExportFileExtension + ")|*." + Defaults.ExportFileExtension + "|All files (*.*)|*.*";

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
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Invalid_File"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
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
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            }
        }

        private void NewNote(object param)
        {
            bool createUnfiled = Convert.ToBoolean(param);

            try
            {
                string initialTitle = this.noteService.GetUniqueNoteTitle(ResourceUtils.GetStringResource("Language_New_Note"));

                FlowDocument flowDoc = new FlowDocument();

                Notebook theNotebook = default(Notebook);

                if (!createUnfiled & (this.SelectedNotebook != null && !this.SelectedNotebook.IsDefaultNotebook))
                {
                    theNotebook = this.SelectedNotebook.Notebook;
                }
                else
                {
                    theNotebook = new Notebook
                    {
                        Title = ResourceUtils.GetStringResource("Language_Unfiled_Notes"),
                        Id = "1",
                        CreationDate = DateTime.Now.Ticks,
                        IsDefaultNotebook = true
                    };
                }

                NoteWindow notewin = new NoteWindow(initialTitle, "", theNotebook, this.searchService.SearchText, true, this.appearanceService, this.jumpListService, this.eventAggregator, this.noteService,
                this.dialogService);

                notewin.ActivateNow();

                RefreshNotes();
            }
            catch (Exception)
            {
                this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Problem_Creating_Note"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
            }
        }

        private void NewNotebook()
        {
            this.selectedNotebook = null;

            string responseText = string.Empty;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetStringResource("Language_New_Notebook"), content: ResourceUtils.GetStringResource("Language_New_Notebook_Enter_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

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

                            this.RefreshNotebooks();
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
                            this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                        }
                    }
                    catch (Exception)
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Problem_Creating_Notebook"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            }
            else
            {
                // The user clicked cancel
            }
        }

        private void RefreshAllHandler(object sender, EventArgs e)
        {
            this.RefreshNotebooks();
            this.RefreshNotes();
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
                await Task.Run(() => {
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
                    nb.Title = ResourceUtils.GetStringResource("Language_All_Notes");
                if (nb.Id == "1")
                    nb.Title = ResourceUtils.GetStringResource("Language_Unfiled_Notes");
            }

            if (this.SelectedNotebook != null)
            {
                if (this.SelectedNotebook.Id == "0")
                    this.SelectedNotebook.Title = ResourceUtils.GetStringResource("Language_All_Notes");
                if (this.SelectedNotebook.Id == "1")
                    this.SelectedNotebook.Title = ResourceUtils.GetStringResource("Language_Unfiled_Notes");

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

        public void RefreshNotebooks()
        {
            this.Notebooks = null;
            var localNotebooks = new ObservableCollection<NotebookViewModel>();

            localNotebooks.Add(new NotebookViewModel
            {
                Notebook = new Notebook
                {
                    Title = ResourceUtils.GetStringResource("Language_All_Notes"),
                    Id = "0",
                    CreationDate = DateTime.Now.Ticks,
                    IsDefaultNotebook = true
                },
                FontWeight = "Bold",
                IsDragOver = false
            });

            localNotebooks.Add(new NotebookViewModel
            {
                Notebook = new Notebook
                {
                    Title = ResourceUtils.GetStringResource("Language_Unfiled_Notes"),
                    Id = "1",
                    CreationDate = DateTime.Now.Ticks,
                    IsDefaultNotebook = true
                },
                FontWeight = "Bold",
                IsDragOver = false
            });

            foreach (Notebook nb in this.noteService.GetNotebooks(ref this.totalNotebooks))
            {
                localNotebooks.Add(new NotebookViewModel
                {
                    Notebook = nb,
                    FontWeight = "Normal",
                    IsDragOver = false
                });
            }

            this.Notebooks = localNotebooks;

            // Because we cannot pass a Property by reference aboves
            OnPropertyChanged(() => this.TotalNotebooks);

            // This makes sure the View is notified that the Notebooks collection has changed. If this call is missing,
            // the list of Notebooks is not updated in the View after we changed its elements here.
            //OnPropertyChanged("Notebooks")
            this.eventAggregator.GetEvent<NotebooksChangedEvent>().Publish("");
        }

        public void RefreshNotes()
        {
            this.Notes = new ObservableCollection<NoteViewModel>();

            string formatDate = "D";
            string text = "";

            if (this.searchService.SearchText != null)
            {
                text = this.searchService.SearchText.Trim();
            }

            if (this.SelectedNotebook == null)
            {
                SetDefaultSelectedNotebook();
            }

            this.Notes.Clear();

            foreach (Note note in this.noteService.GetNotes(new Notebook
            {
                Id = SelectedNotebook.Id,
                Title = SelectedNotebook.Title,
                CreationDate = SelectedNotebook.CreationDate.Ticks
            }, text, ref this.total, SettingsClient.Get<bool>("Appearance", "SortByModificationDate"), this.NoteFilter))
            {
                this.Notes.Add(new NoteViewModel
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

        private void SetDefaultSelectedNotebook()
        {
            try
            {
                this.SelectedNotebook = new NotebookViewModel
                {
                    Notebook = new Notebook
                    {
                        Title = ResourceUtils.GetStringResource("Language_All_Notes"),
                        Id = "0",
                        CreationDate = DateTime.Now.Ticks,
                        IsDefaultNotebook = true
                    }
                };
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not set selected notebook. Exception: {0}", ex.Message);
            }
        }

        public void RefreshNotesAnimated()
        {
            this.RefreshNotes();
            this.eventAggregator.GetEvent<TriggerLoadNoteAnimationEvent>().Publish("");
        }
        #endregion

        #region Commands

        // Toggle note flag
        public void ToggleNoteFlagFromListExecute(object obj)
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

                //helper.CountNotes(Me.AllNotesCounter, Me.TodayNotesCounter, Me.YesterdayNotesCounter, Me.ThisWeekNotesCounter, Me.FlaggedCounter)
            }
        }

        public bool CanToggleNoteFlagFromListExecute(object obj)
        {
            return true;
        }

        public DelegateCommand<object> ToggleNoteFlagFromList
        {
            get { return new DelegateCommand<object>(ToggleNoteFlagFromListExecute, CanToggleNoteFlagFromListExecute); }
        }


        private void DeleteNotebookAction(Notebook notebook)
        {
            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetStringResource("Language_Delete_Notebook"), content: ResourceUtils.GetStringResource("Language_Delete_Notebook_Confirm").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));

            if (dialogResult)
            {
                this.noteService.DeleteNotebook(notebook.Id);
                this.RefreshNotebooks();
                this.SelectedNotebook = new NotebookViewModel
                {
                    Notebook = new Notebook
                    {
                        Id = "0",
                        Title = ResourceUtils.GetStringResource("Language_All_Notes"),
                        CreationDate = DateTime.Now.Ticks,
                        IsDefaultNotebook = true
                    }
                };
            }
        }

        // Delete Notebook
        public void DeleteNotebookExecute()
        {
            if (this.SelectedNotebook != null)
            {
                DeleteNotebookAction(this.SelectedNotebook.Notebook);
            }
            else
            {
                // This should never happen
                MessageBox.Show(messageBoxText: "The notebook could not be deleted.", caption: "Error", button: MessageBoxButton.OK, icon: MessageBoxImage.Error);
            }
        }

        public bool CanDeleteNotebookExecute()
        {
            return true;
        }

        public ICommand DeleteNotebook
        {
            get { return new DelegateCommand(DeleteNotebookExecute, CanDeleteNotebookExecute); }
        }


        public void DeleteNotebookFromListExecute(object obj)
        {
            DeleteNotebookAction(this.noteService.GetNotebook(obj as string));
        }

        public bool CanDeleteNotebookFromListExecute(object obj)
        {
            return true;
        }

        public DelegateCommand<object> DeleteNotebookFromList
        {
            get { return new DelegateCommand<object>(DeleteNotebookFromListExecute, CanDeleteNotebookFromListExecute); }
        }


        public void EditNotebookFromListExecute(object obj)
        {
            Notebook notebook = this.noteService.GetNotebook(obj as string);

            string responseText = notebook.Title;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetStringResource("Language_Edit_Notebook"), content: ResourceUtils.GetStringResource("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.noteService.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.noteService.UpdateNotebook(notebook.Id, responseText);

                        this.RefreshNotebooks();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            }
            else
            {
                // The user clicked Cancel
            }
        }

        public bool CanEditNotebookFromListExecute(object obj)
        {
            return true;
        }

        public DelegateCommand<object> EditNotebookFromList
        {
            get { return new DelegateCommand<object>(EditNotebookFromListExecute, CanEditNotebookFromListExecute); }
        }

        // delete note
        public void DeleteNoteExecute()
        {
            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, title: ResourceUtils.GetStringResource("Language_Delete_Note"), content: ResourceUtils.GetStringResource("Language_Delete_Note_Confirm").Replace("%notename%", this.SelectedNote.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));


            if (dialogResult)
            {
                this.noteService.DeleteNote(this.SelectedNote.Id);

                try
                {
                    this.RefreshNotes();
                }
                catch (Exception)
                {
                }

                this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
            }
        }

        public bool CanDeleteNoteExecute()
        {
            return true;
        }

        public ICommand DeleteNote
        {
            get { return new DelegateCommand(DeleteNoteExecute, CanDeleteNoteExecute); }
        }

        // Edit Notebook
        public void EditNotebookExecute()
        {
            string responseText = this.selectedNotebook.Title;
            bool dialogResult = this.dialogService.ShowInputDialog(null, title: ResourceUtils.GetStringResource("Language_Edit_Notebook"), content: ResourceUtils.GetStringResource("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", this.selectedNotebook.Title), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.noteService.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.noteService.UpdateNotebook(this.SelectedNotebook.Title, responseText);

                        this.RefreshNotebooks();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            }
            else
            {
                // The user clicked Cancel
            }
        }

        public bool CanEditNotebookExecute()
        {
            return true;
        }

        public ICommand EditNotebook
        {
            get { return new DelegateCommand(EditNotebookExecute, CanEditNotebookExecute); }
        }
        #endregion
    }
}