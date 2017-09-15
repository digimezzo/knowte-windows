using System;
using System.IO;
using System.Reflection;

namespace Knowte.Common.Database
{
    public class DbMigrator
    {
        #region DatabaseVersionAttribute
        protected sealed class DatabaseVersionAttribute : Attribute
        {
            private int version;

            public int Version
            {
                get { return this.version; }
            }

            public DatabaseVersionAttribute(int version)
            {
                this.version = version;
            }
        }
        #endregion

        #region Variables
        // NOTE: whenever there is a change in the database schema,
        // this version MUST be incremented and a migration method
        // MUST be supplied to match the new version number
        protected const int CURRENT_VERSION = 1;
        private int userDatabaseVersion;
        private SQLiteConnectionFactory factory;
        #endregion

        #region Properties
        public SQLiteConnectionFactory Factory
        {
            get { return this.factory; }
        }

        public string DatabaseFile
        {
            get
            {
                if(this.factory != null) return this.factory.DatabaseFile;
                return string.Empty;
            }
        }
        #endregion

        #region Construction
        public DbMigrator()
        {
            this.factory = new SQLiteConnectionFactory();
        }

        public DbMigrator(string storageLocation)
        {
            this.factory = new SQLiteConnectionFactory(storageLocation);
        }
        #endregion

        #region Fresh database setup
        private void CreateConfiguration()
        {
            using (var conn = this.factory.GetConnection())
            {
                // conn.CreateTable<Configuration>(); // We choose to create the tables manually
                conn.Execute("CREATE TABLE Configuration (" +
                             "Id                 INTEGER," +
                             "Key                TEXT," +
                             "Value              TEXT," +
                             "PRIMARY KEY(Id));");

                conn.Execute("INSERT INTO Configuration (Key, Value) VALUES ('DatabaseVersion', @versionParam)", CURRENT_VERSION);
            }
        }

        private void CreateTablesAndIndexes()
        {
            using (var conn = this.factory.GetConnection())
            {
                conn.Execute("CREATE TABLE Notebook (" +
                             "Id                        TEXT," +
                             "Title     	            TEXT," +
                             "CreationDate              INTEGER," +
                             "PRIMARY KEY(Id));");

                conn.Execute("CREATE TABLE Note (" +
                                     "Id                TEXT," +
                                     "NotebookId     	TEXT," +
                                     "Title             TEXT," +
                                     "Text              TEXT," +
                                     "CreationDate      INTEGER," +
                                     "OpenDate          INTEGER," +
                                     "ModificationDate  INTEGER," +
                                     "Width             INTEGER," +
                                     "Height            INTEGER," +
                                     "Top               INTEGER," +
                                     "Left              INTEGER, " +
                                     "Flagged           INTEGER," +
                                     "Maximized         INTEGER," +
                                     "PRIMARY KEY(Id));");
            }
        }
        #endregion

        #region Version 1
        [DatabaseVersion(1)]
        private void Migrate1()
        {
            using (var conn = this.factory.GetConnection())
            {
                conn.Execute("DELETE FROM Configuration WHERE Key='NewNoteCount';");
            }
        }
        #endregion

        #region Public
        public bool DatabaseExists()
        {
            return File.Exists(this.factory.DatabaseFile);
        }

        public bool DatabaseNeedsUpgrade()
        {
            using (var conn = this.factory.GetConnection())
            {
                this.userDatabaseVersion = Convert.ToInt32(conn.ExecuteScalar<string>("SELECT Value FROM Configuration WHERE Key = 'DatabaseVersion'"));
            }

            return this.userDatabaseVersion < CURRENT_VERSION;
        }

        public void InitializeNewDatabase()
        {
            this.CreateConfiguration();
            this.CreateTablesAndIndexes();
        }

        public void UpgradeDatabase()
        {
            MethodInfo[] methods = typeof(DbMigrator).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = this.userDatabaseVersion + 1; i <= CURRENT_VERSION; i++)
            {
                foreach (MethodInfo method in methods)
                {
                    foreach (DatabaseVersionAttribute attr in method.GetCustomAttributes(typeof(DatabaseVersionAttribute), false))
                    {
                        if (attr.Version == i)
                        {
                            method.Invoke(this, null);
                        }
                    }
                }
            }

            using (var conn = this.factory.GetConnection())
            {
                conn.Execute("UPDATE Configuration SET Value = ? WHERE Key = 'DatabaseVersion'", CURRENT_VERSION);
            }
        }
        #endregion
    }
}
