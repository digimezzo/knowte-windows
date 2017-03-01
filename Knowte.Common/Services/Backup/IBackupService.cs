using System;

namespace Knowte.Common.Services.Backup
{
    public interface IBackupService
    {
        bool Backup(string backupFile);
        bool CombineRestore(string backupFile);
        bool EraseRestore(string backupFile);
        event EventHandler BackupRestored;
    }
}
