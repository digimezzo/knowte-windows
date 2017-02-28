using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Knowte.Common.Base;
using Knowte.Common.Prism;
using Knowte.Common.Services.Backup;
using Knowte.Common.Services.Dialog;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Knowte.SettingsModule.ViewModels
{
    public class SettingsAdvancedViewModel : BindableBase
    {
        #region Variables
        private IBackupService backupService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<int> numberOfNotesInJumpList;
        private int selectedNumberOfNotesInJumpList;
        #endregion

        #region Commands
        public DelegateCommand BackupCommand { get; set; }
        public DelegateCommand RestoreCommand { get; set; }
        #endregion

        #region Properties
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
        public SettingsAdvancedViewModel(IBackupService backupService, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            // Injection
            this.eventAggregator = eventAggregator;
            this.backupService = backupService;
            this.dialogService = dialogService;

            // Commands
            this.BackupCommand = new DelegateCommand(async () => this.BackupAsync());
            this.RestoreCommand = new DelegateCommand(async () => this.RestoreAsync());

            // Initialize
            this.LoadNumberOfNotesInJumplist();
        }
        #endregion

        #region Private
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

        private async Task BackupAsync()
        {
            bool isBackupSuccess = false;

            // Choose a backup file
            string backupFile = string.Empty;
            bool isBackupFileChosen = this.SaveBackupFile(ref backupFile);
            if (!isBackupFileChosen) return;

            // Perform the backup to file
            isBackupSuccess = await this.backupService.BackupAsync(backupFile);

            // Update LastBackupDirectory setting
            if (isBackupSuccess) SettingsClient.Set<string>("General", "LastBackupDirectory", Path.GetDirectoryName(backupFile));

            // Show error if backup failed
            if (!isBackupSuccess)
            {
                this.dialogService.ShowNotificationDialog(
                    null,
                    iconCharCode: DialogIcons.ErrorIconCode,
                    iconSize: DialogIcons.ErrorIconSize,
                    title: ResourceUtils.GetStringResource("Language_Error"),
                    content: ResourceUtils.GetStringResource("Language_Error_Backup_Error"),
                    okText: ResourceUtils.GetStringResource("Language_Ok"),
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

            // Perform the restore from file
            isRestoreSuccess = await this.backupService.RestoreAsync(backupFile);

            // Show error if restore failed
            if (!isRestoreSuccess)
            {
                this.dialogService.ShowNotificationDialog(
                    null,
                    iconCharCode: DialogIcons.ErrorIconCode,
                    iconSize: DialogIcons.ErrorIconSize,
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