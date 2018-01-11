using Knowte.Common.IO;
using SQLite;

namespace Knowte.Common.Database
{
    public class SQLiteConnectionFactory
    {
        private string customStorageLocation;
      
        public string DatabaseFile
        {
            get
            {
                string storageLocation = string.IsNullOrEmpty(this.customStorageLocation) ? ApplicationPaths.CurrentNoteStorageLocation : this.customStorageLocation;
                return System.IO.Path.Combine(storageLocation, "Notes.db"); ;
            }
        }
        
        public SQLiteConnectionFactory()
        {
        }

        public SQLiteConnectionFactory(string customStorageLocation)
        {
            this.customStorageLocation = customStorageLocation;
        }
       
        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(this.DatabaseFile) { BusyTimeout = new System.TimeSpan(0, 0, 1) };
        }
    }
}
