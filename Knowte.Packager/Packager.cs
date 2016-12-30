using Digimezzo.Utilities.Packaging;
using Knowte.Common.Base;
using System.Reflection;

namespace Knowte.Packager
{
    public class Packager
    {
        static void Main(string[] args)
        {
            Assembly asm = Assembly.GetEntryAssembly();
            AssemblyName an = asm.GetName();

            Configuration config;

#if DEBUG
            config = Configuration.Debug;
#else
		   config = Configuration.Release;
#endif

            var worker = new PackageCreator(ProductInformation.ApplicationDisplayName, an.Version, config);
            worker.Execute();
        }
    }
}
