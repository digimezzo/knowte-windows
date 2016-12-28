using System;

namespace Knowte.Common.Base
{
    public class VersionInfo
    {
        #region Properties
        public Version Version { get; set; }
        public Configuration Configuration { get; set; }
        #endregion

        #region Construction
        public VersionInfo()
        {
            this.Version = new Version(0, 0, 0, 0);
            this.Configuration = Configuration.Debug;
        }
        #endregion
    }
}
