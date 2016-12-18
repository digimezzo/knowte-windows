using Knowte.Core.Base;
using Knowte.Core.Settings;
using SQLite;

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
            return new SQLiteConnection(this.databaseFile);
        }
        #endregion
    }
}
