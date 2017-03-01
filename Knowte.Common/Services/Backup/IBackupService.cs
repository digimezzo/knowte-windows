using System;
using System.Threading.Tasks;

namespace Knowte.Common.Services.Backup
{
    public interface IBackupService
    {
        bool Backup(string backupFile);
        bool MergeRestore(string backupFile);
        bool FullRestore(string backupFile);
        event EventHandler BackupRestored;
    }
}
