using System;

namespace Knowte.Core.Utils
{
    public sealed class EnvironmentUtils
    {
        public static bool IsWindows10()
        {
            // IMPORTANT: Windows 8.1. and Windows 10 will ONLY admit their real version if you program's manifest 
            // claims to be compatible. Otherwise they claim to be Windows 8. See here:
            // https://msdn.microsoft.com/en-us/library/windows/desktop/dn481241(v=vs.85).aspx

            bool returnValue = false;

            // Get Operating system information
            OperatingSystem os = Environment.OSVersion;

            // Get the Operating system version information
            Version vi = os.Version;

            // Pre-NT versions of Windows are PlatformID.Win32Windows. We're not interested in those.

            if (os.Platform == PlatformID.Win32NT)
            {
                if (vi.Major == 10)
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }
    }
}
