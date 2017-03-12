using Knowte.Common.IO;
using SQLite;

namespace Knowte.Common.Database
{
    public class SQLiteConnectionFactory
    {
        #region Private
        private string customStorageLocation;
        #endregion

        #region ReadOnly Properties
        public string DatabaseFile
        {
            get
            {
                string storageLocation = string.IsNullOrEmpty(this.customStorageLocation) ? ApplicationPaths.CurrentNoteStorageLocation : this.customStorageLocation;
                return System.IO.Path.Combine(storageLocation, "Notes.db"); ;
            }
        }
        #endregion

        #region Construction
        public SQLiteConnectionFactory()
        {
        }

        public SQLiteConnectionFactory(string customStorageLocation)
        {
            this.customStorageLocation = customStorageLocation;
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
