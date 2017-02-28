using System;
using System.Threading.Tasks;

namespace Knowte.Common.Services.Backup
{
    public interface IBackupService
    {
        Task<bool> BackupAsync(string backupFile);
        Task<bool> RestoreAsync(string backupFile);
        event EventHandler BackupRestored;
    }
}
