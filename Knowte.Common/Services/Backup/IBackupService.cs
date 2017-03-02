using System;

namespace Knowte.Common.Services.Backup
{
    public interface IBackupService
    {
        bool Backup(string backupFile);
        bool Import(string backupFile);
        bool Restore(string backupFile);
        event EventHandler BackupRestored;
    }
}
