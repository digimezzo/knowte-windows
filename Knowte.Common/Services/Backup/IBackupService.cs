using System;
using System.Threading.Tasks;

namespace Knowte.Common.Services.Backup
{
    public interface IBackupService
    {
        bool Backup(string backupFile);
        bool Restore(string backupFile);
        event EventHandler BackupRestored;
    }
}
