using Knowte.Core.Base;
using Knowte.Core.IO;
using SQLite.Net;
using SQLite.Net.Platform.Win32;
using Knowte.Core.Settings;

namespace Knowte.Core.Database
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
            this.databaseFile = System.IO.Path.Combine(XmlSettingsClient.Instance.ApplicationFolder, ProductInformation.ApplicationAssemblyName + ".db");
        }
        #endregion

        #region Public
        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(new SQLitePlatformWin32(), this.databaseFile);
        }
        #endregion
    }
}
