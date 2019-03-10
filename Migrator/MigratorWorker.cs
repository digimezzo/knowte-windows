using Digimezzo.Utilities.IO;
using Knowte.Common.Base;
using Knowte.Common.Database;
using Knowte.Common.Database.Entities;
using Knowte.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Migrator
{
    public class MigratorWorker
    {
        public MigratorWorker()
        {
        }
     
        public void Execute()
        {
            Console.WriteLine("Migrator");
            Console.WriteLine("========");

            // Workaround so that the Migrator is able to find the Knowte database
            string storageLocation = ApplicationPaths.CurrentNoteStorageLocation.Replace("Migrator", "Knowte");

            var factory = new SQLiteConnectionFactory(storageLocation);
            string notesDatabaseFile = factory.DatabaseFile;

            // Get notebooks
            List<Notebook> notebooks = null;

            using (var conn = factory.GetConnection())
            {
                notebooks = conn.Query<Notebook>("SELECT * FROM Notebook;");
            }

            // Get notes
            List<Note> notes = null;

            using (var conn = factory.GetConnection())
            {
                notes = conn.Query<Note>("SELECT * FROM Note;");
            }

            Console.WriteLine(Environment.NewLine + "Press any key to close this window...");
            Console.ReadKey();
        }
    }
}
