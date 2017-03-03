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
        #region Variables
        private IBackupService backupService;
        private IDialogService dialogService;
        private INoteService noteService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<int> numberOfNotesInJumpList;
        private int selectedNumberOfNotesInJumpList;
        #endregion

        #region Commands
        public DelegateCommand BackupCommand { get; set; }
        public DelegateCommand ImportCommand { get; set; }
        public DelegateCommand RestoreCommand { get; set; }
        public DelegateCommand OpenStorageLocationCommand { get; set; }
        public DelegateCommand ChangeStorageLocationCommand { get; set; }
        public DelegateCommand MoveStorageLocationCommand { get; set; }
        #endregion

        #region Properties
        public string StorageLocation
        {
            get { return ApplicationPaths.NoteStorageLocation; }
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

        #endregion

        #region Construction
        public SettingsAdvancedViewModel(IBackupService backupService, IDialogService dialogService, INoteService noteService, IEventAggregator eventAggregator)
        {
            // Injection
            this.eventAggregator = eventAggregator;
            this.backupService = backupService;
            this.dialogService = dialogService;
            this.noteService = noteService;

            // Storage location
            // TODO

            // Commands
            this.BackupCommand = new DelegateCommand(() => this.Backup());
            this.ImportCommand = new DelegateCommand(() => this.Import());
            this.RestoreCommand = new DelegateCommand(() => this.Restore());
            this.OpenStorageLocationCommand = new DelegateCommand(() => Actions.TryOpenPath(ApplicationPaths.NoteStorageLocation));
            this.ChangeStorageLocationCommand = new DelegateCommand(async () => { await this.ChangeStorageLocationAsync(false); });
            this.MoveStorageLocationCommand = new DelegateCommand(async () => { await this.ChangeStorageLocationAsync(true); });

            // Event handlers
            this.noteService.StorageLocationChanged += (sender, e) => OnPropertyChanged(() => this.StorageLocation);

            // Initialize
            this.LoadNumberOfNotesInJumplist();
        }
        #endregion

        #region Private
        private async Task ChangeStorageLocationAsync(bool moveCurrentNotes)
        {
            var dlg = new WPFFolderBrowserDialog();

            string customStorageLocation = SettingsClient.Get<string>("General", "NoteStorageLocation");

            if (!string.IsNullOrWhiteSpace(customStorageLocation))
            {
                // If there is a custom storage location set, open that location.
                dlg.InitialDirectory = ApplicationPaths.NoteStorageLocation;
            }
            else
            {
                // If there is no custom storage location set, open My Documents.
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            if ((bool)dlg.ShowDialog())
            {
                string selectedFolder = dlg.FileName;
                bool isChangeStorageLocationSuccess = await this.noteService.ChangeStorageLocationAsync(selectedFolder, moveCurrentNotes);

                // Show error if changing storage location failed
                if (isChangeStorageLocationSuccess)
                {
                    // Show notification if change storage location succeeded
                    this.dialogService.ShowNotificationDialog(
                        null,
                        title: ResourceUtils.GetStringResource("Language_Success"),
                        content: ResourceUtils.GetStringResource("Language_Change_Storage_Location_Was_Successful"),
                        okText: ResourceUtils.GetStringResource("Language_Ok"),
                        showViewLogs: false);
                }
                else
                {
                    // Show error if change storage location failed
                    this.dialogService.ShowNotificationDialog(
                      null,
                      title: ResourceUtils.GetStringResource("Language_Error"),
                      content: ResourceUtils.GetStringResource("Language_Error_Change_Storage_Location_Error"),
                      okText: ResourceUtils.GetStringResource("Language_Ok"),
                      showViewLogs: true);
                }
            }
        }

        private bool SaveBackupFile(ref string backupFile)
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");
            dlg.DefaultExt = Defaults.BackupFileExtension;
            dlg.Filter = ProductInformation.ApplicationDisplayName + " backup file (*." + Defaults.BackupFileExtension + ")|*." + Defaults.BackupFileExtension + "|All files (*.*)|*.*";

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
            dlg.Filter = ProductInformation.ApplicationDisplayName + " backup file (*." + Defaults.BackupFileExtension + ")|*." + Defaults.BackupFileExtension + "|All files (*.*)|*.*";

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

        private void Backup()
        {
            bool isBackupSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.SaveBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Perform the backup to file
            isBackupSuccess = this.backupService.Backup(backupFile);

            // Update LastBackupDirectory setting
            if (isBackupSuccess) SettingsClient.Set<string>("General", "LastBackupDirectory", Path.GetDirectoryName(backupFile));

            if (isBackupSuccess)
            {
                // Show notification if backup succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetStringResource("Language_Success"),
                    content: ResourceUtils.GetStringResource("Language_Backup_Was_Successful"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if backup failed
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetStringResource("Language_Error"),
                    content: ResourceUtils.GetStringResource("Language_Error_Backup_Error"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: true);
            }
        }

        private void Import()
        {
            bool isImportSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.OpenBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Perform the restore from file
            isImportSuccess = this.backupService.Import(backupFile);

            if (isImportSuccess)
            {
                // Show notification if import succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetStringResource("Language_Success"),
                    content: ResourceUtils.GetStringResource("Language_Import_Was_Successful"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if import failed
                this.dialogService.ShowNotificationDialog(
                                   null,
                                   title: ResourceUtils.GetStringResource("Language_Error"),
                                   content: ResourceUtils.GetStringResource("Language_Error_Import_Error"),
                                   okText: ResourceUtils.GetStringResource("Language_Ok"),
                                   showViewLogs: true);
            }
        }

        private void Restore()
        {
            bool isRestoreSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.OpenBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Perform the restore from file
            isRestoreSuccess = this.backupService.Restore(backupFile);

            if (isRestoreSuccess)
            {
                // Show notification if restore succeeded
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetStringResource("Language_Success"),
                    content: ResourceUtils.GetStringResource("Language_Restore_Was_Successful"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: false);
            }
            else
            {
                // Show error if restore failed
                this.dialogService.ShowNotificationDialog(
                    null,
                    title: ResourceUtils.GetStringResource("Language_Error"),
                    content: ResourceUtils.GetStringResource("Language_Error_Restore_Error"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
                    showViewLogs: true);
            }
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
        #endregion
    }
}