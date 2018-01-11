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
