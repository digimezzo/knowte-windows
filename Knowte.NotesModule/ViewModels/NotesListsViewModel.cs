using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Database.Entities;
using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.I18n;
using Knowte.Common.Services.Notes;
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
        private INotebookService notebookSevice;
        private INoteService noteService;
        private IAppearanceService appearanceService;
        private IJumpListService jumpListService;
        private ISearchService searchService;
        private IDialogService dialogService;
        private I18nService i18nService;
        private bool triggerRefreshNotesAnimation;
        #endregion

        #region Commands
        public DelegateCommand<string> NewNotebookCommand;
        public DelegateCommand<object> NewNoteCommand;
        public DelegateCommand<string> ImportNoteCommand;
        public DelegateCommand<string> OpenNoteCommand;
        public DelegateCommand<object> NavigateBetweenNotesCommand;
        public DelegateCommand BackupRestoreCommand { get; set; }
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
        public NotesListsViewModel(IEventAggregator eventAggregator, INotebookService notebookSevice, INoteService noteService, IAppearanceService appearanceService, IJumpListService jumpListService, ISearchService searchService, IDialogService dialogService, I18nService i18nService)
        {
            this.eventAggregator = eventAggregator;
            this.notebookSevice = notebookSevice;
            this.noteService = noteService;
            this.appearanceService = appearanceService;
            this.jumpListService = jumpListService;
            this.searchService = searchService;
            this.dialogService = dialogService;
            this.i18nService = i18nService;

            this.eventAggregator.GetEvent<TriggerLoadNoteAnimationEvent>().Subscribe((x) =>
            {
                this.TriggerRefreshNotesAnimation = false;
                this.TriggerRefreshNotesAnimation = true;
            });

            this.eventAggregator.GetEvent<RefreshJumpListEvent>().Subscribe((x) => this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes()));
            this.eventAggregator.GetEvent<NewNoteEvent>().Subscribe(createUnfiled => this.NewNoteCommand.Execute(createUnfiled));
            this.eventAggregator.GetEvent<DeleteNoteEvent>().Subscribe((x) => this.DeleteNote.Execute(null));
            this.eventAggregator.GetEvent<DeleteNotebookEvent>().Subscribe((x) => this.DeleteNotebook.Execute(null));
            this.eventAggregator.GetEvent<RefreshNotesEvent>().Subscribe((x) => this.RefreshNotes());
            this.eventAggregator.GetEvent<OpenNoteEvent>().Subscribe(iNoteTitle =>
            {
                if (!string.IsNullOrEmpty(iNoteTitle))
                {
                    this.SelectedNote = new NoteViewModel { Title = iNoteTitle };
                }

                this.OpenNoteCommand.Execute(null);
            });

            this.i18nService.LanguageChanged += LanguageChangedHandler;

            this.noteService.FlagUpdated += (sender, e) => { this.RefreshNotes(); };

            this.Notes = new ObservableCollection<NoteViewModel>();

            this.RefreshNotebooks();

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
            // New Notebook
            this.NewNotebookCommand = new DelegateCommand<string>((_) =>
            {
                this.selectedNotebook = null;

                string responseText = string.Empty;
                bool dialogResult = this.dialogService.ShowInputDialog(null, iconCharCode: DialogIcons.EditIconCode, iconSize: DialogIcons.EditIconSize, title: ResourceUtils.GetStringResource("Language_New_Notebook"), content: ResourceUtils.GetStringResource("Language_New_Notebook_Enter_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

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

                            if (!this.notebookSevice.NotebookExists(newNotebook))
                            {
                                this.notebookSevice.NewNotebook(newNotebook);

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
                                this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                            }
                        }
                        catch (Exception)
                        {
                            this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Problem_Creating_Notebook"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                        }
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    // The user clicked cancel
                }
            });
            Common.Prism.ApplicationCommands.NewNotebookCommand.RegisterCommand(this.NewNotebookCommand);

            // New Note
            this.NewNoteCommand = new DelegateCommand<object>(iParam =>
            {
                bool createUnfiled = Convert.ToBoolean(iParam);

                try
                {
                    string initialTitle = ResourceUtils.GetStringResource("Language_New_Note") + " " + (Convert.ToString(this.noteService.GetNewNoteCount() + 1).ToString());

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

                    NoteWindow notewin = new NoteWindow(initialTitle, "", theNotebook, this.searchService.SearchText, true, this.appearanceService, this.jumpListService, this.eventAggregator, this.notebookSevice, this.noteService,
                    this.dialogService);

                    this.noteService.IncreaseNewNoteCount();

                    notewin.ActivateNow();

                    RefreshNotes();
                }
                catch (Exception)
                {
                    this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Problem_Creating_Note"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            });
            Common.Prism.ApplicationCommands.NewNoteCommand.RegisterCommand(this.NewNoteCommand);

            // Import Note
            this.ImportNoteCommand = new DelegateCommand<string>((x) =>
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
                            this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Invalid_File"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
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
                        this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }

                }
            });
            Common.Prism.ApplicationCommands.ImportNoteCommand.RegisterCommand(this.ImportNoteCommand);

            this.NavigateBetweenNotesCommand = new DelegateCommand<object>(NavigateBetweenNotes);
            Common.Prism.ApplicationCommands.NavigateBetweenNotesCommand.RegisterCommand(this.NavigateBetweenNotesCommand);

            this.OpenNoteCommand = new DelegateCommand<string>((_) =>
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

                    NoteWindow newWindow = new NoteWindow(this.SelectedNote.Title, "", this.notebookSevice.GetNotebook(this.SelectedNote.NotebookId), this.searchService.SearchText, false, this.appearanceService, this.jumpListService, this.eventAggregator, this.notebookSevice, this.noteService,
                    this.dialogService);

                    ((NoteWindow)newWindow).ActivateNow();
                }
                catch (Exception)
                {
                    // This should never happen
                    this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Note_Could_Not_Be_Opened"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }

            });

            // Backup and restore
            this.BackupRestoreCommand = new DelegateCommand(() => MessageBox.Show("Backup and restore"));

            this.searchService.Searching += (_, __) =>
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() => { this.RefreshNotes(); });
                }
                catch (Exception)
                {
                }
            };

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

            this.NoteFilter = "";
            // Must be set before RefreshNotes()
            this.RefreshNotes();
        }
        #endregion

        #region Private
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

            foreach (Notebook nb in this.notebookSevice.GetNotebooks(ref this.totalNotebooks))
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
            string formatDate = "D";

            string theText = "";

            if (this.searchService.SearchText != null)
            {
                theText = this.searchService.SearchText.Trim();
            }

            if (this.SelectedNotebook == null)
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
                catch (Exception)
                {
                }
            }

            this.Notes.Clear();

            foreach (Note note in this.noteService.GetNotes(new Notebook
            {
                Id = SelectedNotebook.Id,
                Title = SelectedNotebook.Title,
                CreationDate = SelectedNotebook.CreationDate.Ticks
            }, theText, ref this.total, SettingsClient.Get<bool>("Appearance", "SortByModificationDate"), this.NoteFilter))
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

        public void RefreshNotesAnimated()
        {
            this.RefreshNotes();
            this.eventAggregator.GetEvent<TriggerLoadNoteAnimationEvent>().Publish("");
        }
        #endregion

        #region Commands
        // Delete Note
        public void DeleteNoteFromListExecute(object obj)
        {

            if (obj != null)
            {
                Note theNote = this.noteService.GetNote(obj as string);

                bool dialogResult = this.dialogService.ShowConfirmationDialog(null, iconCharCode: DialogIcons.QuestionIconCode, iconSize: DialogIcons.QuestionIconSize, title: ResourceUtils.GetStringResource("Language_Delete_Note"), content: ResourceUtils.GetStringResource("Language_Delete_Note_Confirm").Replace("%notename%", theNote.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));


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

        public bool CanDeleteNoteFromListExecute(object obj)
        {
            return true;
        }

        public DelegateCommand<object> DeleteNoteFromList
        {
            get { return new DelegateCommand<object>(DeleteNoteFromListExecute, CanDeleteNoteFromListExecute); }
        }

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
            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, iconCharCode: DialogIcons.QuestionIconCode, iconSize: DialogIcons.QuestionIconSize, title: ResourceUtils.GetStringResource("Language_Delete_Notebook"), content: ResourceUtils.GetStringResource("Language_Delete_Notebook_Confirm").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));

            if (dialogResult)
            {
                this.notebookSevice.DeleteNotebook(notebook.Id);
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
            DeleteNotebookAction(this.notebookSevice.GetNotebook(obj as string));
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
            Notebook notebook = this.notebookSevice.GetNotebook(obj as string);

            string responseText = notebook.Title;
            bool dialogResult = this.dialogService.ShowInputDialog(null, iconCharCode: DialogIcons.EditIconCode, iconSize: DialogIcons.EditIconSize, title: ResourceUtils.GetStringResource("Language_Edit_Notebook"), content: ResourceUtils.GetStringResource("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", notebook.Title), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.notebookSevice.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.notebookSevice.UpdateNotebook(notebook.Id, responseText);

                        this.RefreshNotebooks();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
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
            bool dialogResult = this.dialogService.ShowConfirmationDialog(null, iconCharCode: DialogIcons.QuestionIconCode, iconSize: DialogIcons.QuestionIconSize, title: ResourceUtils.GetStringResource("Language_Delete_Note"), content: ResourceUtils.GetStringResource("Language_Delete_Note_Confirm").Replace("%notename%", this.SelectedNote.Title), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));


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
            bool dialogResult = this.dialogService.ShowInputDialog(null, iconCharCode: DialogIcons.EditIconCode, iconSize: DialogIcons.EditIconSize, title: ResourceUtils.GetStringResource("Language_Edit_Notebook"), content: ResourceUtils.GetStringResource("Language_Enter_New_Name_For_Notebook").Replace("%notebookname%", this.selectedNotebook.Title), okText: ResourceUtils.GetStringResource("Language_Ok"), cancelText: ResourceUtils.GetStringResource("Language_Cancel"), responeText: ref responseText);

            if (dialogResult)
            {

                if (!string.IsNullOrEmpty(responseText))
                {

                    if (!this.notebookSevice.NotebookExists(new Notebook { Title = responseText }))
                    {
                        this.notebookSevice.UpdateNotebook(this.SelectedNotebook.Title, responseText);

                        this.RefreshNotebooks();
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Notebook_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.dialogService.ShowNotificationDialog(null, iconCharCode: DialogIcons.ErrorIconCode, iconSize: DialogIcons.ErrorIconSize, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Notebook_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
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