using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Settings;
using Knowte.Common.Base;
using Knowte.Common.IO;
using SQLite;

namespace Knowte.Common.Database
{
    public class SQLiteConnectionFactory
    {
        #region Private
        private string databaseFile;
        #endregion

        #region ReadOnly Properties
        public string DatabaseFile
        {
            get { return this.databaseFile; }
        }
        #endregion

        #region Construction
        public SQLiteConnectionFactory()
        {
            this.databaseFile = System.IO.Path.Combine(ApplicationPaths.NoteStorageLocation, ProcessExecutable.Name() + ".db");
        }

        public SQLiteConnectionFactory(string storageLocation)
        {
            this.databaseFile = System.IO.Path.Combine(storageLocation, ProcessExecutable.Name() + ".db");
        }
        #endregion

        #region Public
        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(this.databaseFile) { BusyTimeout = new System.TimeSpan(0, 0, 1) };
        }
        #endregion
    }
}
