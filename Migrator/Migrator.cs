using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migrator
{
    class Migrator
    {
        static void Main(string[] args)
        {
            var worker = new MigratorWorker();
            worker.Execute();
        }
    }
}
