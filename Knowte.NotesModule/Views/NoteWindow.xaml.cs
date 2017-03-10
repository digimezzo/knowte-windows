using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.Controls;
using Knowte.Common.Database.Entities;
using Knowte.Common.Extensions;
using Knowte.Common.Prism;
using Knowte.Common.Services.Appearance;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.Note;
using Knowte.Common.Services.WindowsIntegration;
using Knowte.Common.Utils;
using Microsoft.Win32;
using Prism.Events;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Knowte.NotesModule.Views
{
    public partial class NoteWindow : KnowteWindow
    {
        #region Variables
        private Notebook notebook;
        private string initialTitle;
        private Timer saveTimer = new Timer();
        private Timer typingTimer = new Timer();
        private string id;
        private double saveSleepSeconds = 5;
        private double typingSeconds = 0.2;
        private bool isContentChanged = false;
        private bool isParametersChanged = false;
        private bool justOpened = true;
        private bool flagged;
        private string copiedText;
        private Timer searchTimer = new Timer();
        private double searchSleepSeconds = 0.2;
        private IAppearanceService appearanceService;
        private IJumpListService jumpListService;
        private IEventAggregator eventAggregator;
        private INoteService noteService;
        private IDialogService dialogService;
        #endregion

        #region Properties
        public new object DataContext
        {
            get { return base.DataContext; }
            set { base.DataContext = value; }
        }

        public ObservableCollection<Rect> SearchRectangles
        {
            get { return (ObservableCollection<Rect>)GetValue(SearchRectanglesProperty); }

            set { SetValue(SearchRectanglesProperty, value); }
        }

        public string InitialTitle
        {
            get { return this.initialTitle; }
            set { this.initialTitle = value; }
        }

        public string Id
        {
            get { return this.id; }
            set { this.id = value; }
        }

        public string CopiedText
        {
            get { return this.copiedText; }
            set { this.copiedText = value; }
        }

        public bool JustOpened
        {
            get { return this.justOpened; }
            set { this.justOpened = value; }
        }
        #endregion

        #region Dependency properties
        public static readonly DependencyProperty SearchRectanglesProperty = DependencyProperty.Register("SearchRectangles", typeof(ObservableCollection<Rect>), typeof(NoteWindow), new PropertyMetadata(null));
        #endregion

        #region Construction
        public NoteWindow(string title, string id, Notebook notebook, string searchText, bool isNew, IAppearanceService appearanceService, IJumpListService jumpListService, IEventAggregator eventAggregator, INoteService noteService, IDialogService dialogService)
        {
            // This call is required by the designer.
            InitializeComponent();

            // Initalize Collections
            this.SearchRectangles = new ObservableCollection<Rect>();

            // Services
            this.appearanceService = appearanceService;
            this.jumpListService = jumpListService;
            this.eventAggregator = eventAggregator;
            this.noteService = noteService;
            this.dialogService = dialogService;

            // Initial button status
            this.SetButtonsStatus();

            // Event Handling
            this.appearanceService.AppearanceChanged += AppearanceChangedHandler;

            // Timers
            this.saveTimer.Interval = TimeSpan.FromSeconds(this.saveSleepSeconds).TotalMilliseconds;
            this.saveTimer.Elapsed += new ElapsedEventHandler(this.SaveTimerHandler);

            this.typingTimer.Interval = TimeSpan.FromSeconds(this.typingSeconds).TotalMilliseconds;
            this.typingTimer.Elapsed += new ElapsedEventHandler(this.TypingTimerHandler);

            this.searchTimer.Interval = TimeSpan.FromSeconds(this.searchSleepSeconds).TotalMilliseconds;
            this.searchTimer.Elapsed += new ElapsedEventHandler(this.SearchTimerHandler);

            // Events
            this.eventAggregator.GetEvent<NotebooksChangedEvent>().Subscribe((x) => { this.RefreshNotebooks(); });

            this.eventAggregator.GetEvent<RefreshNotebooksEvent>().Subscribe((x) => { this.RefreshNotebooks(); });


            this.InitialTitle = title;
            this.notebook = notebook;

            // Always hide the search panel when opening a note
            this.ShowHideSearchPanel(false, true);

            if (!isNew)
            {
                // We open an existing note
                Note note = this.noteService.GetNote(title);
                this.Id = note.Id;
                this.flagged = note.Flagged == 1 ? true : false;

                InitWindowExisting();

                // Set the text of the SearchBox
                if (!string.IsNullOrEmpty(searchText))
                {
                    this.ShowHideSearchPanel(true, true);
                    this.SearchBox.Text = searchText;
                }
            }
            else
            {
                // We open a new note
                if (string.IsNullOrEmpty(id))
                {
                    this.Id = Guid.NewGuid().ToString();
                }
                else
                {
                    this.Id = id;
                }

                InitWindowNew();

                // ALways set the SearchBox text to "" for new notes
                this.SearchBox.Text = "";
            }

            // This must happen AFTER the RichtTextBox is filled with text
            this.SubscribeToAllHyperlinks();

            // Fill the combobox with notesbooks for the first time
            this.RefreshNotebooks();

            // Clear the formatting of pasted text
            // See: http://social.msdn.microsoft.com/Forums/vstudio/en-US/0d672c70-d49d-4ebf-871d-420cc164f7d8/c-wpf-richtextbox-remove-formatting-and-line-spaces
            DataObject.AddPastingHandler(XAMLRichTextBox, new DataObjectPastingEventHandler(XAMLRichTextBoxPasting));
            DataObject.AddCopyingHandler(XAMLRichTextBox, new DataObjectCopyingEventHandler(XAMLRichTextBoxCopying));
            DataObject.AddPastingHandler(TextBoxTitle, new DataObjectPastingEventHandler(TextBoxTitlePasting));

            // Is the note flagged?
            this.SetFlagVisibility();
        }
        #endregion

        #region Private
        private void SetFlagVisibility()
        {
            if (this.flagged)
            {
                this.UnFlaggedIcon.Visibility = Visibility.Hidden;
                this.FlaggedIcon.Visibility = Visibility.Visible;
            }
            else
            {
                this.UnFlaggedIcon.Visibility = Visibility.Visible;
                this.FlaggedIcon.Visibility = Visibility.Hidden;
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (this.SearchPanel.Opacity == 0)
            {
                this.ShowHideSearchPanel(true, false);
                this.SearchBox.Focus();
            }
            else
            {
                this.ShowHideSearchPanel(false, false);
            }
        }

        private void BtnFlag_Click(object sender, RoutedEventArgs e)
        {
            this.flagged = !this.flagged;
            this.noteService.UpdateNoteFlag(this.id, this.flagged);
            this.SetFlagVisibility();
        }

        private void RefreshNotebooks()
        {
            this.NotebooksComboBox.Items.Clear();
            this.NotebooksComboBox.Items.Add(new Notebook
            {
                Title = "Unfiled notes",
                Id = "1",
                CreationDate = DateTime.Now.Ticks,
                IsDefaultNotebook = true
            });

            foreach (Notebook nb in this.noteService.GetNotebooks())
            {
                this.NotebooksComboBox.Items.Add(nb);
            }

            this.NotebooksComboBox.SelectedItem = this.notebook;
        }

        private void ShowHideSearchPanel(bool iShow, bool iInstantaneous)
        {
            int fromHeight = 0;
            int toHeight = 0;
            int fromOpacity = 0;
            int toOpacity = 0;
            Thickness fromMargin = new Thickness(10, 0, 10, 0);
            Thickness toMargin = new Thickness(10, 0, 10, 0);

            double sizeSeconds = 0.1;
            double opacitySeconds = 0.5;

            if (iShow)
            {
                toHeight = 28;
                toOpacity = 1;
                toMargin = new Thickness(10);
            }
            else
            {
                fromHeight = 28;
                fromOpacity = 1;
                fromMargin = new Thickness(10);
            }

            if (iInstantaneous)
            {
                sizeSeconds = 0;
                opacitySeconds = 0;
            }

            // Height
            DoubleAnimation heightAnimation = new DoubleAnimation();
            heightAnimation.From = fromHeight;
            heightAnimation.To = toHeight;
            heightAnimation.Duration = new Duration(TimeSpan.FromSeconds(sizeSeconds));

            // Opacity
            DoubleAnimation opacityAnimation = new DoubleAnimation();
            opacityAnimation.From = fromOpacity;
            opacityAnimation.To = toOpacity;
            opacityAnimation.Duration = new Duration(TimeSpan.FromSeconds(opacitySeconds));

            // Margin
            ThicknessAnimation marginAnimation = new ThicknessAnimation();
            marginAnimation.From = fromMargin;
            marginAnimation.To = toMargin;
            marginAnimation.Duration = new Duration(TimeSpan.FromSeconds(sizeSeconds));

            // StoryBoard
            Storyboard myStoryboard = default(Storyboard);
            myStoryboard = new Storyboard();
            myStoryboard.Children.Add(heightAnimation);
            myStoryboard.Children.Add(opacityAnimation);
            myStoryboard.Children.Add(marginAnimation);

            // Targets
            Storyboard.SetTargetName(heightAnimation, this.SearchPanel.Name);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
            Storyboard.SetTargetName(opacityAnimation, this.SearchPanel.Name);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(FrameworkElement.OpacityProperty));
            Storyboard.SetTargetName(marginAnimation, this.SearchPanel.Name);
            Storyboard.SetTargetProperty(marginAnimation, new PropertyPath(FrameworkElement.MarginProperty));
            myStoryboard.Begin(this);

        }

        private void InitWindowExisting()
        {
            // Titles
            this.Title = this.InitialTitle;
            this.TextBoxTitle.Text = this.InitialTitle;

            // Get the note's information

            var theNote = this.noteService.GetNote(this.InitialTitle);

            this.SetGeometry(theNote.Top, theNote.Left, theNote.Width, theNote.Height, Defaults.DefaultNoteTop, Defaults.DefaultNoteLeft);

            if (theNote.Maximized == 1)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }

            LoadNoteResult result = this.noteService.LoadNote(XAMLRichTextBox.Document, theNote);

            if (result == LoadNoteResult.Success)
            {
                FontFamily normalFont = new FontFamily(Defaults.NoteFont);

                foreach (Block block in XAMLRichTextBox.Document.Blocks)
                {
                    block.Foreground = Brushes.Black;
                    block.FontFamily = normalFont;
                    block.FontSize = Defaults.DefaultNoteFontSize + SettingsClient.Get<int>("Notes", "FontSizeCorrection");
                }

                XAMLRichTextBox.Document.Foreground = Brushes.Black;
                XAMLRichTextBox.Document.FontFamily = normalFont;
                XAMLRichTextBox.Document.FontSize = Defaults.DefaultNoteFontSize + SettingsClient.Get<int>("Notes", "FontSizeCorrection");

                XAMLRichTextBox.IsUndoEnabled = false;
                XAMLRichTextBox.IsUndoEnabled = true;

                this.VisualizeUndoState();

                this.noteService.UpdateOpenDate(this.Id);
                this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
            }
            else
            {
                this.dialogService.ShowNotificationDialog(
                    this,
                    title: ResourceUtils.GetStringResource("Language_Error"),
                    content: ResourceUtils.GetStringResource("Language_Could_Not_Open_Note"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: true);

                this.Close();
            }
        }

        private void InitWindowNew()
        {
            XAMLRichTextBox.Document = new FlowDocument();

            // Titles
            this.Title = this.InitialTitle;
            TextBoxTitle.Text = this.InitialTitle;

            this.SetGeometry(Defaults.DefaultNoteTop, Defaults.DefaultNoteLeft, Defaults.DefaultNoteWidth, Defaults.DefaultNoteHeight, Defaults.DefaultNoteTop, Defaults.DefaultNoteLeft);

            XAMLRichTextBox.Document.FontSize = Defaults.DefaultNoteFontSize + SettingsClient.Get<int>("Notes", "FontSizeCorrection");

            // Immediately save the note

            // Save the new note for the first time (SaveNote is not required here)
            this.noteService.NewNote(XAMLRichTextBox.Document, this.Id, this.InitialTitle, this.noteService.GetNotebookId(this.notebook.Title));

            FontFamily normalFont = new FontFamily(Defaults.NoteFont);

            foreach (Block block in XAMLRichTextBox.Document.Blocks)
            {
                block.Foreground = Brushes.Black;
                block.FontFamily = normalFont;
                block.FontSize = Defaults.DefaultNoteFontSize + SettingsClient.Get<int>("Notes", "FontSizeCorrection");
            }

            XAMLRichTextBox.IsUndoEnabled = false;
            XAMLRichTextBox.IsUndoEnabled = true;

            this.VisualizeUndoState();

            this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
        }

        private bool SaveNote()
        {
            bool retVal = false;

            try
            {
                if (this.InitialTitle.Equals(this.TextBoxTitle.Text) | !this.noteService.NoteExists(this.TextBoxTitle.Text))
                {
                    if (!this.TextBoxTitle.Text.Trim().Equals(""))
                    {
                        bool isMaximized = false;

                        if (this.WindowState == WindowState.Maximized)
                        {
                            isMaximized = true;
                        }

                        if (this.isContentChanged)
                        {
                            this.noteService.UpdateNote(XAMLRichTextBox.Document, this.Id, this.TextBoxTitle.Text, this.noteService.GetNotebookId(this.notebook.Title), this.ActualWidth, this.ActualHeight, this.Top, this.Left, isMaximized);
                            this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());
                        }
                        else
                        {
                            this.noteService.UpdateNoteParameters(this.Id, this.ActualWidth, this.ActualHeight, this.Top, this.Left, isMaximized);
                        }

                        this.InitialTitle = this.TextBoxTitle.Text;
                        this.isContentChanged = false;
                        this.isParametersChanged = false;

                        retVal = true;
                    }
                    else
                    {
                        this.TextBoxTitle.Text = this.InitialTitle;
                        this.dialogService.ShowNotificationDialog(this, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Note_Needs_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.TextBoxTitle.Text = this.InitialTitle;
                    this.dialogService.ShowNotificationDialog(this,title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Already_Note_With_That_Name"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                }
            }
            catch (Exception ex)
            {
                this.TextBoxTitle.Text = this.InitialTitle;
                this.dialogService.ShowNotificationDialog(this, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                LogClient.Error("An error occured while saving the Note '{0}'. Exception: {1}", this.TextBoxTitle.Text, ex.Message);

                retVal = false;
            }

            return retVal;
        }
        #endregion

        #region Events
        // Clears the formatting of pasted text
        // See: http://social.msdn.microsoft.com/Forums/vstudio/en-US/0d672c70-d49d-4ebf-871d-420cc164f7d8/c-wpf-richtextbox-remove-formatting-and-line-spaces
        // Improved using: http://go4answers.webhost4life.com/Example/changing-wpf-richtextbox-foreground-190295.aspx

        private void XAMLRichTextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                string lPastingText = e.DataObject.GetData(DataFormats.Text) as string;

                // Check if we are pasting text. If not, lPastingText Is Nothing
                if (lPastingText != null)
                {

                    if (!lPastingText.Equals(this.CopiedText))
                    {
                        if (!e.SourceDataObject.GetDataPresent(DataFormats.Rtf, true))
                        {
                            return;
                        }
                        var rtf = e.SourceDataObject.GetData(DataFormats.Rtf) as string;

                        FlowDocument document = new FlowDocument();
                        document.SetValue(FlowDocument.TextAlignmentProperty, TextAlignment.Left);

                        TextRange content = new TextRange(document.ContentStart, document.ContentEnd);

                        if (content.CanLoad(DataFormats.Rtf) && string.IsNullOrEmpty(rtf) == false)
                        {
                            // If so then load it with RTF
                            byte[] valueArray = Encoding.ASCII.GetBytes(rtf);
                            using (MemoryStream stream = new MemoryStream(valueArray))
                            {
                                content.Load(stream, DataFormats.Rtf);
                            }
                        }

                        DataObject d = new DataObject();
                        //d.SetData(DataFormats.Text, content.Text.Replace(Environment.NewLine, Constants.vbLf));
                        d.SetData(DataFormats.Text, content.Text); // TODO
                        e.DataObject = d;
                    }
                }
            }
            catch (Exception)
            {
            }

        }

        private void XAMLRichTextBoxCopying(object sender, DataObjectCopyingEventArgs e)
        {
            try
            {
                this.CopiedText = e.DataObject.GetData(DataFormats.Text) as string;
            }
            catch (Exception)
            {
            }
        }

        private void TextBoxTitlePasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                string lPastingText = e.DataObject.GetData(DataFormats.Text) as string;

                // Check if we are pasting text. If not, lPastingText Is Nothing
                if (lPastingText != null)
                {
                    e.CancelCommand();

                    int currentCarretIndex = 0;

                    string cleanedText = MiscUtils.RemoveBullets(lPastingText);

                    if (TextBoxTitle.SelectedText != null && TextBoxTitle.SelectedText.Length > 0)
                    {
                        currentCarretIndex = TextBoxTitle.SelectionStart;
                        TextBoxTitle.SelectedText = cleanedText;
                    }
                    else
                    {
                        currentCarretIndex = TextBoxTitle.CaretIndex;
                        TextBoxTitle.Text = TextBoxTitle.Text.Insert(TextBoxTitle.CaretIndex, cleanedText);
                    }

                    TextBoxTitle.CaretIndex = currentCarretIndex + cleanedText.Length;

                }
            }
            catch (Exception)
            {
            }
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            XAMLRichTextBox.Undo();
            this.VisualizeUndoState();
        }

        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            XAMLRichTextBox.Redo();
            this.VisualizeUndoState();
        }

        private void MetroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            this.VisualizeUndoState();

            if (e.Key == Key.Escape)
            {
                if (SettingsClient.Get<bool>("Notes", "PressingEscapeClosesNotes"))
                {
                    this.Close();
                }

            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (this.SearchPanel.Opacity == 0)
                {
                    this.ShowHideSearchPanel(true, false);
                    this.SearchBox.Focus();
                }
                else
                {
                    this.ShowHideSearchPanel(false, false);
                }

            }
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Print
                this.DoPrint();
            }
        }

        private void RefreshSearch()
        {
            if (!string.IsNullOrEmpty(this.SearchBox.Text))
            {
                // Update the search highlight
                Debug.WriteLine("Refreshing search");
                this.HighLightSearch();
            }
            else
            {
                Debug.WriteLine("Not refreshing search");
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // We stop the timer, as we'll force a save here
            this.saveTimer.Stop();
            //Stop the timer

            if (this.isContentChanged | this.isParametersChanged)
            {
                if (!SaveNote())
                {
                    e.Cancel = true;
                }
            }
        }

        private async void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            bool dialogResult = this.dialogService.ShowConfirmationDialog(this,title: ResourceUtils.GetStringResource("Language_Delete_Note"), content: ResourceUtils.GetStringResource("Language_Delete_Note_Confirm").Replace("%notename%", this.TextBoxTitle.Text), okText: ResourceUtils.GetStringResource("Language_Yes"), cancelText: ResourceUtils.GetStringResource("Language_No"));


            if (dialogResult)
            {
                await this.noteService.DeleteNoteAsync(this.id);

                this.isContentChanged = false;  // Prevents saving changes when deleting a note (because that generates an exception)
                this.isParametersChanged = false; // Prevents saving changes when deleting a note (because that generates an exception)

                this.jumpListService.RefreshJumpListAsync(this.noteService.GetRecentlyOpenedNotes(SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList")), this.noteService.GetFlaggedNotes());

                this.Close();
            }
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // TODO: this function changes the last modified date I think, not good
            // JustOpened prevents starting the save timer when a note is just opened (a resize happens when opening)
            if (!JustOpened)
            {
                this.isParametersChanged = true;

                this.saveTimer.Stop();
                this.saveTimer.Start(); //Start the timer
            }
        }

        private void NotebooksComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotebooksComboBox.SelectedItem != null)
            {
                this.notebook = (Notebook)NotebooksComboBox.SelectedItem;

                if (!JustOpened)
                {
                    this.isContentChanged = true;

                    this.saveTimer.Stop();
                    this.saveTimer.Start(); //Start the timer
                }
            }
        }

        private void DoHighlight()
        {
            try
            {
                bool highLight = true;

                object br_obj = XAMLRichTextBox.Selection.GetPropertyValue(TextElement.BackgroundProperty);

                if (br_obj is SolidColorBrush)
                {
                    if (((SolidColorBrush)br_obj).Color.Equals(Colors.Yellow))
                    {
                        highLight = false;
                    }
                }

                if (highLight)
                {
                    XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: TextElement.BackgroundProperty, value: Brushes.Yellow);
                }
                else
                {
                    XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: TextElement.BackgroundProperty, value: Brushes.Transparent);
                }

                this.isContentChanged = true;
                VisualizeUndoState();

                this.saveTimer.Stop();
                this.saveTimer.Start();
                //Start the timer
                Debug.WriteLine("Save timer (re)started");
            }
            catch (Exception)
            {
                Debug.WriteLine("A problem occured while trying to edit Highlight");
            }
        }

        private void DoStrikeout()
        {
            try
            {
                TextDecorationCollection tdc = new TextDecorationCollection();
                tdc = XAMLRichTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty) as TextDecorationCollection;

                if (tdc != null && tdc.Count == 0)
                {
                    XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: Inline.TextDecorationsProperty, value: TextDecorations.Strikethrough);
                }
                else
                {
                    // This also clears underline: should we care? (ToggleUnderline also clears Strikethrough...)
                    XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: Inline.TextDecorationsProperty, value: new TextDecorationCollection());
                }

                this.isContentChanged = true;
                VisualizeUndoState();

                this.saveTimer.Stop();
                this.saveTimer.Start();
                //Start the timer
                Debug.WriteLine("Save timer (re)started");
            }
            catch (Exception)
            {
                Debug.WriteLine("A problem occured while trying to edit Strike through");
            }

        }

        private void XAMLRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!JustOpened)
            {
                // Making sure there are actual changes to the text. Because searching triggers this function when the note is just opened

                if (e.Changes.Count > 0)
                {
                    this.typingTimer.Stop();
                    this.typingTimer.Start();

                    this.isContentChanged = true;

                    this.saveTimer.Stop();
                    this.saveTimer.Start();
                    //Start the timer
                    Debug.WriteLine("Resized, save timer (re)started");
                }
            }
        }

        private void Link_MouseDown(object sender, EventArgs e)
        {
            this.Topmost = false;
            //Me.WindowState = Windows.WindowState.Normal

            try
            {
                Hyperlink link = (Hyperlink)sender;


                if (link.NavigateUri.ToString().StartsWith("file://"))
                {
                    Process.Start(link.NavigateUri.ToString());

                }
                else if (link.NavigateUri.ToString().StartsWith("http://") | link.NavigateUri.ToString().StartsWith("https://") | link.NavigateUri.ToString().StartsWith("mailto://"))
                {
                    Process.Start(link.NavigateUri.ToString());

                }
                else if (link.NavigateUri.ToString().StartsWith("note://"))
                {
                    string theGuid = link.NavigateUri.ToString().Replace("note://", "").Replace("/", "");

                    if (this.noteService.NoteIdExists(theGuid))
                    {
                        Note theNote = this.noteService.GetNoteById(theGuid);
                        this.eventAggregator.GetEvent<OpenNoteEvent>().Publish(theNote.Title);
                    }
                    else
                    {
                        this.dialogService.ShowNotificationDialog(this, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Link_Does_Not_Work_Anymore"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        private void Link_MouseEnter(object sender, EventArgs e)
        {
            XAMLRichTextBox.Cursor = Cursors.Hand;
        }

        private void Link_MouseLeave(object sender, EventArgs e)
        {
            XAMLRichTextBox.Cursor = Cursors.IBeam;
        }


        private void XAMLRichTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // This stop inserting a "Tab" when pressing Ctrl+T. We want to assign Ctrl+T to "fixed width"
            if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
            }
            else if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                // we implement our own
            }
            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                // we implement our own
            }
            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                // we implement our own
            }

            TextPointer caretPosition = XAMLRichTextBox.Selection.Start;


            if (e.Key == Key.Space | e.Key == Key.Enter)
            {
                TextPointer wordStartPosition = default(TextPointer);
                string word = MiscUtils.GetPreceedingWordInParagraph(caretPosition, ref wordStartPosition);


                if (MiscUtils.IsUrl(word) | MiscUtils.IsMail(word))
                {
                    // Insert hyperlink element at word boundaries.

                    // No need to update RichTextBox caret position, 
                    // since we only inserted a Hyperlink ElementEnd following current caretPosition.
                    // Subsequent handling of space input by base RichTextBox will update selection.

                    try
                    {
                        Uri tryUri = default(Uri);

                        if (MiscUtils.IsMail(word) & !word.StartsWith("mailto://"))
                        {
                            tryUri = new Uri("mailto://" + word);
                        }
                        else if (MiscUtils.IsUrl(word))
                        {
                            // http:// and https:// is mandatory and already added
                            tryUri = new Uri(word);
                        }

                        Hyperlink link = new Hyperlink(wordStartPosition.GetPositionAtOffset(0, LogicalDirection.Backward), caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward))
                        {
                            IsEnabled = true,
                            NavigateUri = tryUri,
                            Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(ResourceUtils.GetStringResource("RG_AccentColor")) }
                        };

                        link.MouseDown += this.Link_MouseDown;
                        link.MouseEnter += this.Link_MouseEnter;
                        link.MouseLeave += this.Link_MouseLeave;
                    }
                    catch (Exception)
                    {
                        return;
                    }

                }
            }
            else if (e.Key == Key.Back)
            {
                TextPointer backspacePosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Backward);
                Hyperlink hyperlink = default(Hyperlink);
                if (backspacePosition != null && MiscUtils.IsHyperlinkBoundaryCrossed(caretPosition, backspacePosition, ref hyperlink))
                {
                    // Remember caretPosition with forward gravity. This is necessary since we are going to delete 
                    // the hyperlink element preceeding caretPosition and after deletion current caretPosition 
                    // (with backward gravity) will follow content preceeding the hyperlink. 
                    // We want to remember content following the hyperlink to set new caret position at.

                    TextPointer newCaretPosition = caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);

                    // Deleting the hyperlink is done using logic below.

                    // 1. Copy its children Inline to a temporary array.
                    InlineCollection hyperlinkChildren = hyperlink.Inlines;
                    Inline[] inlines = new Inline[hyperlinkChildren.Count];
                    hyperlinkChildren.CopyTo(inlines, 0);

                    // 2. Remove each child from parent hyperlink element and insert it after the hyperlink.
                    for (int i = inlines.Length - 1; i >= 0; i += -1)
                    {
                        hyperlinkChildren.Remove(inlines[i]);
                        hyperlink.SiblingInlines.InsertAfter(hyperlink, inlines[i]);
                    }

                    // 3. Apply hyperlink's local formatting properties to inlines (which are now outside hyperlink scope).
                    LocalValueEnumerator localProperties = hyperlink.GetLocalValueEnumerator();
                    TextRange inlineRange = new TextRange(inlines[0].ContentStart, inlines[inlines.Length - 1].ContentEnd);

                    while (localProperties.MoveNext())
                    {
                        LocalValueEntry property = localProperties.Current;
                        DependencyProperty dp = property.Property;
                        object value = property.Value;

                        // Ignore hyperlink defaults.
                        // RG: for a, yet unknown, reason I need a try catch here, inlineRange.ApplyPropertyValue(dp, value) fails sometimes
                        try
                        {
                            if (!dp.ReadOnly && !dp.Equals(Inline.TextDecorationsProperty) && !dp.Equals(TextElement.ForegroundProperty) && !MiscUtils.IsHyperlinkProperty(dp))
                            {
                                inlineRange.ApplyPropertyValue(dp, value);
                            }

                        }
                        catch (Exception)
                        {
                        }

                    }

                    // 4. Delete the (empty) hyperlink element.
                    hyperlink.SiblingInlines.Remove(hyperlink);

                    // 5. Update selection, since we deleted Hyperlink element and caretPosition was at that Hyperlink's end boundary.
                    XAMLRichTextBox.Selection.Select(newCaretPosition, newCaretPosition);
                }
            }

        }

        private void XAMLRichTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            SetButtonsStatus();

            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoStrikeout();
            }
            else if (e.Key == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoHighlight();
            }
            else if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoBold();
            }
            else if (e.Key == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoItalic();
            }
            else if (e.Key == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoUnderline();

            }
            else if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                //' Workaround for Bug: "Copy from richtextbox text sometimes pastes into richtextbox as transparent text"
                //For Each block As Block In XAMLRichTextBox.Document.Blocks
                //    If block.Foreground.ToString.Equals("#00FFFFFF") Then
                //        block.Foreground = Brushes.Black
                //    End If
                //Next
            }
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.DoFixedWidth();
            }
            else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.CreateNoteLink();
            }
        }

        private void CreateNoteLink()
        {
            TextRange tr = new TextRange(XAMLRichTextBox.Selection.Start, XAMLRichTextBox.Selection.End);

            string theText = tr.Text;

            string theGuid = Guid.NewGuid().ToString();

            if (this.noteService.NoteExists(theText))
            {
                theGuid = this.noteService.GetNote(theText).Id;
            }

            this.CreateLinkedNote(MiscUtils.RemoveBullets(theText), theGuid, this.notebook);

            Uri tryUri = default(Uri);
            tryUri = new Uri("note://" + theGuid);

            Hyperlink link = new Hyperlink(XAMLRichTextBox.Selection.Start, XAMLRichTextBox.Selection.End)
            {
                IsEnabled = true,
                NavigateUri = tryUri,
                Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(ResourceUtils.GetStringResource("RG_AccentColor")) }
            };

            link.MouseDown += this.Link_MouseDown;
            link.MouseEnter += this.Link_MouseEnter;
            link.MouseLeave += this.Link_MouseLeave;
        }

        private void CreateLinkedNote(string title, string id, Notebook notebook)
        {
            if (title != null && id != null && !title.Equals("") && !id.Equals(""))
            {
                if (!this.noteService.NoteExists(title))
                {
                    try
                    {
                        NoteWindow notewin = new NoteWindow(title, id, notebook, "", true, this.appearanceService, this.jumpListService, this.eventAggregator, this.noteService,
                        this.dialogService);
                        notewin.Show();
                        this.noteService.OnNotesChanged(); // TODO: can this be done better?
                    }
                    catch (Exception)
                    {
                        this.dialogService.ShowNotificationDialog(this, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Problem_Creating_Note"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: false);
                    }
                }
                else
                {
                    this.eventAggregator.GetEvent<OpenNoteEvent>().Publish(title);
                }
            }
        }

        private void DoFixedWidth()
        {
            try
            {
                FontFamily normalFont = new FontFamily(Defaults.NoteFont);
                FontFamily fixedFont = new FontFamily(Defaults.NoteFWFont);

                object br_obj = XAMLRichTextBox.Selection.GetPropertyValue(TextElement.FontFamilyProperty);

                if (br_obj is FontFamily)
                {
                    if (((FontFamily)br_obj).Equals(fixedFont))
                    {
                        XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: Inline.FontFamilyProperty, value: normalFont);
                    }
                    else
                    {
                        XAMLRichTextBox.Selection.ApplyPropertyValue(formattingProperty: Inline.FontFamilyProperty, value: fixedFont);
                    }
                }

                VisualizeUndoState();
            }
            catch (Exception)
            {
                Debug.WriteLine("A problem occured while trying to edit Fixed Width");
            }

        }

        private void TextBoxTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Title = TextBoxTitle.Text;

            if (!JustOpened)
            {
                this.isContentChanged = true;

                this.saveTimer.Stop();
                this.saveTimer.Start(); //Start the timer
                Debug.WriteLine("Resized, save timer (re)started");
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Enable blur
            if (EnvironmentUtils.IsWindows10())
            {
                this.BottomControlsBackground.Opacity = Constants.OpacityWhenBlurred;
                WindowUtils.EnableBlur(this);
            }
            else
            {
                this.BottomControlsBackground.Opacity = 1.0;
            }

            this.JustOpened = false;
        }

        private void VisualizeUndoState()
        {
            if (XAMLRichTextBox.CanUndo)
            {
                BtnUndo.IsEnabled = true;
            }
            else
            {
                BtnUndo.IsEnabled = false;
            }

            if (XAMLRichTextBox.CanRedo)
            {
                BtnRedo.IsEnabled = true;
            }
            else
            {
                BtnRedo.IsEnabled = false;
            }
        }

        private void MetroWindow_LocationChanged(object sender, EventArgs e)
        {
            // TODO: this function changes the last modified date I think, not good
            // JustOpened prevents starting the save timer when a note is just opened (a resize happens when opening)
            if (!JustOpened)
            {
                this.isParametersChanged = true;

                this.saveTimer.Stop();
                this.saveTimer.Start(); //Start the timer
                Debug.WriteLine("Resized, save timer (re)started");
            }
        }

        public bool IsRichTextBoxEmpty()
        {
            if (XAMLRichTextBox.Document.Blocks.Count == 0)
                return true;
            TextPointer startPointer = XAMLRichTextBox.Document.ContentStart.GetNextInsertionPosition(LogicalDirection.Forward);
            TextPointer endPointer = XAMLRichTextBox.Document.ContentEnd.GetNextInsertionPosition(LogicalDirection.Backward);
            return startPointer.CompareTo(endPointer) == 0;
        }

        private void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            this.Topmost = false;
        }

        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            foreach (Window win in Application.Current.Windows)
            {
                win.Topmost = false;
            }
        }

        // Hyperlink stuff
        public void SubscribeToAllHyperlinks()
        {
            var hyperlinks = VisualTreeUtils.GetVisuals(this.XAMLRichTextBox.Document).OfType<Hyperlink>();

            foreach (Hyperlink link in hyperlinks)
            {
                link.MouseDown += this.Link_MouseDown;
                link.MouseEnter += this.Link_MouseEnter;
                link.MouseLeave += this.Link_MouseLeave;
                link.Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(ResourceUtils.GetStringResource("RG_AccentColor")) };
            }
        }

        private void BtnLink_Click(object sender, RoutedEventArgs e)
        {
            this.CreateNoteLink();
        }

        private bool HasSelectedText()
        {

            bool returnVal = false;

            TextRange tr = new TextRange(XAMLRichTextBox.Selection.Start, XAMLRichTextBox.Selection.End);

            if (tr.Text.Length > 0)
            {
                returnVal = true;
            }

            return returnVal;
        }

        private void SetButtonsStatus()
        {
            if (this.HasSelectedText())
            {
                BtnLink.IsEnabled = true;
            }
            else
            {
                BtnLink.IsEnabled = false;
            }
        }

        private void XAMLRichTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            SetButtonsStatus();
        }

        private void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.XAMLRichTextBox.IsUndoEnabled = false;
            this.XAMLRichTextBox.UndoLimit = 0;
        }

        private void DoBold()
        {
            EditingCommands.ToggleBold.Execute(null, this.XAMLRichTextBox);
            this.isContentChanged = true;
            VisualizeUndoState();
        }

        private void DoItalic()
        {
            EditingCommands.ToggleItalic.Execute(null, this.XAMLRichTextBox);
            this.isContentChanged = true;
            VisualizeUndoState();
        }

        private void DoUnderline()
        {
            EditingCommands.ToggleUnderline.Execute(null, this.XAMLRichTextBox);
            this.isContentChanged = true;
            VisualizeUndoState();
        }

        private void DoList()
        {
            EditingCommands.ToggleBullets.Execute(null, this.XAMLRichTextBox);
            this.isContentChanged = true;
            VisualizeUndoState();
        }

        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            this.DoBold();
        }

        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            this.DoItalic();
        }

        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            this.DoUnderline();
        }

        private void BtnStrikeout_Click(object sender, RoutedEventArgs e)
        {
            this.DoStrikeout();
        }

        private void ButtonFixedWidth_Click(object sender, RoutedEventArgs e)
        {
            this.DoFixedWidth();
        }

        private void ButtonHighlight_Click(object sender, RoutedEventArgs e)
        {
            this.DoHighlight();
        }

        private void BtnList_Click(object sender, RoutedEventArgs e)
        {
            this.DoList();
        }


        private void BtnExportRtf_Click(object sender, RoutedEventArgs e)
        {
            this.SaveNote();

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Export to RTF";
            dlg.Filter = "Rich Text Format|*.rtf";

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

            try
            {
                dlg.FileName = this.Title.SanitizeFilename() + ".rtf";

                if ((bool)dlg.ShowDialog())
                {
                    this.noteService.ExportToRtf(this.Id, this.Title, dlg.FileName);

                    SettingsClient.Set<string>("General", "LastExportDirectory", Path.GetDirectoryName(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not export note to rtf. Exception: {0}", ex.Message);
                this.dialogService.ShowNotificationDialog(this,title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: true);
            }
        }

        private void BtnExportNote_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Export note";
            dlg.Filter = "Note|*." + Defaults.ExportFileExtension;

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

            try
            {
                dlg.FileName = this.Title.SanitizeFilename() + "." + Defaults.ExportFileExtension;

                if ((bool)dlg.ShowDialog())
                {
                    this.noteService.ExportFile(this.Id, dlg.FileName);

                    SettingsClient.Set<string>("General", "LastExportDirectory", Path.GetDirectoryName(dlg.FileName));
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not export note. Exception: {0}", ex.Message);
                this.dialogService.ShowNotificationDialog(this, title: ResourceUtils.GetStringResource("Language_Error"), content: ResourceUtils.GetStringResource("Language_Error_Unexpected_Error"), okText: ResourceUtils.GetStringResource("Language_Ok"), showViewLogs: true);
            }
        }

        private void DoPrint()
        {
            this.SaveNote();

            Window realMainWindow = Application.Current.MainWindow;

            // Workaround for the Printdialog not having a "owner" property
            // Set the current notewindow as the main window
            Application.Current.MainWindow = this;

            this.noteService.Print(this.Id, this.Title);

            // Restore the real mainwindow
            Application.Current.MainWindow = realMainWindow;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            this.DoPrint();
        }

        private void XAMLRichTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.RefreshSearchAfterScroll();
        }

        private void XAMLRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Prevents dissapearing of selections when the richtextbox loses focus
            e.Handled = true;
        }

        private void TextBoxTitle_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                // Sets the focus to the XAMLRichTextBox
                XAMLRichTextBox.Focus();

                // Puts the caret at the start of the XAMLRichTextBox
                XAMLRichTextBox.CaretPosition = XAMLRichTextBox.Document.ContentStart;

                // Prevents adding an extra return to the XAMLRichTextBox
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.searchTimer.Stop();
            this.searchTimer.Start();
        }
        #endregion

        #region Public
        public void SaveTimerHandler(object sender, ElapsedEventArgs e)
        {
            this.saveTimer.Stop(); //Stop the timer

            Debug.WriteLine("Saved " + this.Id + ", save timer stopped");

            if (this.isContentChanged | this.isParametersChanged)
            {
                this.Dispatcher.Invoke(new Func<bool>(SaveNote));
            }
        }

        public void TypingTimerHandler(object sender, ElapsedEventArgs e)
        {
            this.typingTimer.Stop(); //Stop the timer

            this.Dispatcher.Invoke(new Action(RefreshSearch));
        }

        public void SearchTimerHandler(object sender, ElapsedEventArgs e)
        {
            this.searchTimer.Stop(); //Stop the timer

            Application.Current.Dispatcher.Invoke(new Action(HighLightSearch));
        }


        public void DoSearch(TextRange range)
        {
            Rect leftRectangle = range.Start.GetCharacterRect(LogicalDirection.Forward);
            Rect rightRectangle = range.End.GetCharacterRect(LogicalDirection.Backward);

            Rect rect = new Rect(leftRectangle.TopLeft, rightRectangle.BottomRight);

            Point translatedPoint = this.XAMLRichTextBox.TranslatePoint(new Point(0, 0), null);
            Point endPoint = this.XAMLRichTextBox.TranslatePoint(new Point(this.XAMLRichTextBox.ActualWidth, this.XAMLRichTextBox.ActualHeight), null);
            rect.Offset(translatedPoint.X - 1, translatedPoint.Y - 1);

            if (rect.X >= translatedPoint.X - 1 & rect.X <= endPoint.X & rect.Y >= translatedPoint.Y - 1 & rect.Y <= (endPoint.Y - 10))
            {
                this.SearchRectangles.Add(rect);
            }
        }

        public void ClearSearch()
        {
            this.SearchRectangles.Clear();
        }

        public void HighLightSearch()
        {
            //Me.ApplyTextEffect(New TextRange(XAMLRichTextBox.Document.ContentStart, XAMLRichTextBox.Document.ContentEnd), New SolidColorBrush(Colors.Black))
            ClearSearch();

            //Dim hyperlinks = GetVisuals(Me.XAMLRichTextBox.Document).OfType(Of Hyperlink)()

            //For Each link As Hyperlink In hyperlinks
            //    Me.ApplyTextEffect(New TextRange(link.ContentStart, link.ContentEnd), New SolidColorBrush With {.Color = CType(ColorConverter.ConvertFromString(Me.LinkColor), Color)})
            //Next

            if (this.SearchBox.Text == null || this.SearchBox.Text.Equals(""))
            {
                return;
            }

            foreach (string word in this.SearchBox.Text.Trim().Split(' '))
            {
                // Filters out double spaces: we don't want to search for spaces in the text

                if (!word.Trim().Equals(""))
                {
                    // Then, search for occurences of "word"
                    TextPointer position = XAMLRichTextBox.Document.ContentStart;

                    while (position != null)
                    {

                        if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                        {
                            string textInRun = position.GetTextInRun(LogicalDirection.Forward).ToLower();

                            // TODO: can we improve this? The interwebz doesn't care enough to provide a solution

                            while (textInRun.ToLower().Contains(word.ToLower()))
                            {
                                int indexInRun = textInRun.IndexOf(word.ToLower());

                                if (indexInRun >= 0)
                                {
                                    position = position.GetPositionAtOffset(indexInRun);

                                    TextRange tr = new TextRange(position, position.GetPositionAtOffset(word.Length));

                                    //Me.ApplyTextEffect(tr, New SolidColorBrush(Colors.Red))
                                    DoSearch(tr);

                                    position = tr.End;
                                    textInRun = position.GetTextInRun(LogicalDirection.Forward).ToLower();
                                }
                            }
                        }

                        position = position.GetNextContextPosition(LogicalDirection.Forward);
                        //position = position.GetPositionAtOffset(word.Length)
                    }
                }
            }
        }

        //Private Sub ApplyTextEffect(range As TextRange, brush As SolidColorBrush)

        //    Dim textPointer As TextPointer = range.Start

        //    ' http://www.nullskull.com/a/1446/wpf-customized-find-control-for-flowdocuments.aspx
        //    Dim effect As New TextEffect()

        //    effect.Foreground = brush

        //    If TypeOf range.Start.Parent Is Inline Then
        //        Dim parent As Inline = TryCast(range.Start.Parent, Inline)

        //        effect.PositionStart = Math.Abs(range.Start.GetOffsetToPosition(parent.ContentStart))
        //        effect.PositionCount = Math.Abs(range.[End].GetOffsetToPosition(range.Start))

        //    ElseIf TypeOf range.Start.Parent Is Block Then
        //        Dim parent As Block = TryCast(range.Start.Parent, Block)
        //        effect.PositionStart = Math.Abs(range.Start.GetOffsetToPosition(parent.ContentStart))
        //        effect.PositionCount = Math.Abs(range.[End].GetOffsetToPosition(range.Start))
        //    End If

        //    Dim targets As TextEffectTarget() = TextEffectResolver.Resolve(range.Start, range.[End], effect)

        //    For Each target As TextEffectTarget In targets
        //        target.Enable()
        //    Next
        //End Sub

        public void RefreshSearchAfterScroll()
        {
            this.ClearSearch();
            this.searchTimer.Start();
        }

        private void HideSearchIcon_Click(object sender, RoutedEventArgs e)
        {
            this.SearchBox.Text = "";
            this.ShowHideSearchPanel(false, false);
        }

        public void AppearanceChangedHandler(object sender, EventArgs e)
        {
            var hyperlinks = VisualTreeUtils.GetVisuals(this.XAMLRichTextBox.Document).OfType<Hyperlink>();

            foreach (Hyperlink link in hyperlinks)
            {
                link.Foreground = new SolidColorBrush { Color = (Color)ColorConverter.ConvertFromString(ResourceUtils.GetStringResource("RG_AccentColor")) };
            }
        }
        #endregion
    }
}