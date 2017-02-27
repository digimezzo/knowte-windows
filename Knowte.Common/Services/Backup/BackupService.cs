using System.Threading.Tasks;

namespace Knowte.Common.Services.Backup
{
    public class BackupService : IBackupService
    {
        #region IBackupService
        public async Task<bool> BackupAsync(string backupFile)
        {
            // TODO: implement
            return false;
        }

        public async Task<bool> RestoreAsync(string backupFile)
        {
            // TODO: implement
            return false;
        }
        #endregion
    }
}
