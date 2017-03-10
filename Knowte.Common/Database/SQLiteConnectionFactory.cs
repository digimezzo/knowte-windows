using Digimezzo.Utilities.IO;
using Knowte.Common.IO;
using SQLite;

namespace Knowte.Common.Database
{
    public class SQLiteConnectionFactory
    {
        #region Private
        private string customDatabaseFile;
        #endregion

        #region ReadOnly Properties
        public string DatabaseFile
        {
            get {
                if (!string.IsNullOrEmpty(this.customDatabaseFile))
                {
                    return this.customDatabaseFile;
                }

                return System.IO.Path.Combine(ApplicationPaths.NoteStorageLocation, ProcessExecutable.Name() + ".db"); ;
            }
        }
        #endregion

        #region Construction
        public SQLiteConnectionFactory()
        {
        }

        public SQLiteConnectionFactory(string customStorageLocation)
        {
            this.customDatabaseFile = System.IO.Path.Combine(customStorageLocation, ProcessExecutable.Name() + ".db");
        }
        #endregion

        #region Public
        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(this.DatabaseFile) { BusyTimeout = new System.TimeSpan(0, 0, 1) };
        }
        #endregion
    }
}
