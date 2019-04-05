using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Base;
using Knowte.Common.IO;
using Knowte.Common.Prism;
using Knowte.Common.Services.Backup;
using Knowte.Common.Services.Dialog;
using Knowte.Common.Services.Note;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using WPFFolderBrowser;

namespace Knowte.SettingsModule.ViewModels
{
    public class SettingsAdvancedViewModel : BindableBase
    {
        private IBackupService backupService;
        private IDialogService dialogService;
        private INoteService noteService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<int> numberOfNotesInJumpList;
        private int selectedNumberOfNotesInJumpList;
        private bool checkBoxChangeStorageLocationFromMainChecked;

        public DelegateCommand ExportCommand { get; set; }
        public DelegateCommand BackupCommand { get; set; }
        public DelegateCommand ImportCommand { get; set; }
        public DelegateCommand RestoreCommand { get; set; }
        public DelegateCommand OpenStorageLocationCommand { get; set; }
        public DelegateCommand ChangeStorageLocationCommand { get; set; }
        public DelegateCommand MoveStorageLocationCommand { get; set; }
        public DelegateCommand ResetStorageLocationCommand { get; set; }

        public bool CheckBoxChangeStorageLocationFromMainChecked
        {
            get { return this.checkBoxChangeStorageLocationFromMainChecked; }
            set
            {
                SetProperty<bool>(ref this.checkBoxChangeStorageLocationFromMainChecked, value);

                SettingsClient.Set<bool>("Advanced", "ChangeStorageLocationFromMain", value);
                this.eventAggregator.GetEvent<SettingChangeStorageLocationFromMainChangedEvent>().Publish("");
            }
        }

        public string StorageLocation
        {
            get { return ApplicationPaths.CurrentNoteStorageLocation; }
        }

        public ObservableCollection<int> NumberOfNotesInJumpList
        {
            get
            {
                return this.numberOfNotesInJumpList;
            }
            set
            {
                SetProperty<ObservableCollection<int>>(ref this.numberOfNotesInJumpList, value);
            }
        }

        public int SelectedNumberOfNotesInJumpList
        {
            get
            {
                return this.selectedNumberOfNotesInJumpList;
            }
            set
            {
                SetProperty<int>(ref this.selectedNumberOfNotesInJumpList, value);
                SettingsClient.Set<int>("Advanced", "NumberOfNotesInJumpList", value);
                this.eventAggregator.GetEvent<RefreshJumpListEvent>().Publish("");
            }
        }

        public SettingsAdvancedViewModel(IBackupService backupService, IDialogService dialogService, INoteService noteService, IEventAggregator eventAggregator)
        {
            // Injection
            this.eventAggregator = eventAggregator;
            this.backupService = backupService;
            this.dialogService = dialogService;
            this.noteService = noteService;

            // Commands
            this.ExportCommand = new DelegateCommand(async () => await this.ExportAsync());
            this.BackupCommand = new DelegateCommand(async () => await this.BackupAsync());
            this.ImportCommand = new DelegateCommand(async () => await this.ImportAsync());
            this.RestoreCommand = new DelegateCommand(async () => await this.RestoreAsync());
            this.OpenStorageLocationCommand = new DelegateCommand(() => Actions.TryOpenPath(ApplicationPaths.CurrentNoteStorageLocation));
            this.ChangeStorageLocationCommand = new DelegateCommand(async () => { await this.ChangeStorageLocationAsync(false, false); });
            this.MoveStorageLocationCommand = new DelegateCommand(async () => { await this.ChangeStorageLocationAsync(false, true); });
            this.ResetStorageLocationCommand = new DelegateCommand(async () => { await this.ChangeStorageLocationAsync(true, false); });

            // Event handlers
            this.noteService.StorageLocationChanged += (sender, e) => OnPropertyChanged(() => this.StorageLocation);

            // Initialize
            this.LoadNumberOfNotesInJumplist();
            this.LoadCheckBoxStates();
        }

        private async Task ChangeStorageLocationAsync(bool performReset, bool moveCurrentNotes)
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

            // Close all note windows
            await this.noteService.CloseAllNoteWindowsAsync(500);

            bool isChangeStorageLocationSuccess = await this.noteService.ChangeStorageLocationAsync(selectedFolder, moveCurrentNotes);

            // Show error if changing storage location failed
            if (isChangeStorageLocationSuccess)
            {
                // Show notification if change storage location succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Success"),
                    content: ResourceUtils.GetString("Language_Change_Storage_Location_Was_Successful"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if change storage location failed
                this.dialogService.ShowNotificationDialog(
                  null,
                  title: ResourceUtils.GetString("Language_Error"),
                  content: ResourceUtils.GetString("Language_Error_Change_Storage_Location_Error"),
                  okText: ResourceUtils.GetString("Language_Ok"),
                  showViewLogs: true);
            }
        }

        private bool SaveBackupFile(ref string backupFile)
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");
            dlg.DefaultExt = Defaults.BackupFileExtension;
            dlg.Filter = ProductInformation.ApplicationName + " backup file (*." + Defaults.BackupFileExtension + ")|*." + Defaults.BackupFileExtension + "|All files (*.*)|*.*";

            string lastBackupDirectory = SettingsClient.Get<string>("General", "LastBackupDirectory");

            if (!string.IsNullOrEmpty(lastBackupDirectory))
            {
                dlg.InitialDirectory = lastBackupDirectory;
            }
            else
            {
                // If no LastBackupDirectory is set, default to the My Documents folder.
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if ((bool)dlg.ShowDialog())
            {
                backupFile = dlg.FileName;
                return true;
            }

            return false;
        }

        private bool OpenBackupFile(ref string backupFile)
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = Defaults.BackupFileExtension;
            dlg.Filter = ProductInformation.ApplicationName + " backup file (*." + Defaults.BackupFileExtension + ")|*." + Defaults.BackupFileExtension + "|All files (*.*)|*.*";

            string lastBackupDirectory = SettingsClient.Get<string>("General", "LastBackupDirectory");

            if (!string.IsNullOrEmpty(lastBackupDirectory))
            {
                dlg.InitialDirectory = lastBackupDirectory;
            }
            else
            {
                // If no LastBackupDirectory is set, default to the My Documents folder.
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if ((bool)dlg.ShowDialog())
            {
                backupFile = dlg.FileName;
                return true;
            }

            return false;
        }
   
        private async Task ExportAsync()
        {
            // Close all note windows
            await this.noteService.CloseAllNoteWindowsAsync(500);

            string exportLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            var dlg = new WPFFolderBrowserDialog();
            dlg.InitialDirectory = exportLocation;

            if ((bool)dlg.ShowDialog())
            {
                exportLocation = dlg.FileName;

                bool isExportSuccess = this.backupService.Export(exportLocation);

                //// Show error if exporting failed
                if (isExportSuccess)
                {
                    // Show notification if exporting succeeded
                    this.dialogService.ShowNotificationDialog(
                        null,
                        title: ResourceUtils.GetString("Language_Success"),
                        content: ResourceUtils.GetString("Language_Export_Was_Successful"),
                        okText: ResourceUtils.GetString("Language_Ok"),
                        showViewLogs: false);
                }
                else
                {
                    // Show error if exporting failed
                    this.dialogService.ShowNotificationDialog(
                      null,
                      title: ResourceUtils.GetString("Language_Error"),
                      content: ResourceUtils.GetString("Language_Export_Error"),
                      okText: ResourceUtils.GetString("Language_Ok"),
                      showViewLogs: true);
                }
            }
        }

        private async Task BackupAsync()
        {
            bool isBackupSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.SaveBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Close all note windows
            await this.noteService.CloseAllNoteWindowsAsync(500);

            // Perform the backup to file
            isBackupSuccess = this.backupService.Backup(backupFile);

            // Update LastBackupDirectory setting
            if (isBackupSuccess) SettingsClient.Set<string>("General", "LastBackupDirectory", Path.GetDirectoryName(backupFile));

            if (isBackupSuccess)
            {
                // Show notification if backup succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Success"),
                    content: ResourceUtils.GetString("Language_Backup_Was_Successful"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if backup failed
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Error"),
                    content: ResourceUtils.GetString("Language_Error_Backup_Error"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: true);
            }
        }

        private async Task ImportAsync()
        {
            bool isImportSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.OpenBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Close all note windows
            await this.noteService.CloseAllNoteWindowsAsync(500);

            // Perform the restore from file
            isImportSuccess = this.backupService.Import(backupFile);

            if (isImportSuccess)
            {
                // Show notification if import succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Success"),
                    content: ResourceUtils.GetString("Language_Import_Was_Successful"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if import failed
                this.dialogService.ShowNotificationDialog(
                                   null,
                                   title: ResourceUtils.GetString("Language_Error"),
                                   content: ResourceUtils.GetString("Language_Error_Import_Error"),
                                   okText: ResourceUtils.GetString("Language_Ok"),
                                   showViewLogs: true);
            }
        }

        private async Task RestoreAsync()
        {
            bool isRestoreSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.OpenBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Close all note windows
            await this.noteService.CloseAllNoteWindowsAsync(500);

            // Perform the restore from file
            isRestoreSuccess = this.backupService.Restore(backupFile);

            if (isRestoreSuccess)
            {
                // Show notification if restore succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Success"),
                    content: ResourceUtils.GetString("Language_Restore_Was_Successful"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if restore failed
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetString("Language_Error"),
                    content: ResourceUtils.GetString("Language_Error_Restore_Error"),
                    okText: ResourceUtils.GetString("Language_Ok"),
                    showViewLogs: true);
            }
        }

        private void LoadCheckBoxStates()
        {
            this.CheckBoxChangeStorageLocationFromMainChecked = SettingsClient.Get<bool>("Advanced", "ChangeStorageLocationFromMain");
        }

        private void LoadNumberOfNotesInJumplist()
        {
            this.numberOfNotesInJumpList = new ObservableCollection<int>();

            for (int i = 0; i <= 10; i++)
            {
                this.NumberOfNotesInJumpList.Add(i);
            }

            this.SelectedNumberOfNotesInJumpList = SettingsClient.Get<int>("Advanced", "NumberOfNotesInJumpList");
        }
    }
}