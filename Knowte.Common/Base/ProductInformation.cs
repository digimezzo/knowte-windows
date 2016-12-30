using System.Reflection;
using System;
using Knowte.Common.Helpers;

namespace Knowte.Common.Base
{
    public class ProductInformation
    {
        public static string ApplicationGuid = "e3be6998-dbcf-4f99-b2b5-bf046fe680f7";
        public static string ApplicationDisplayName = "Knowte";
        public static string ApplicationDisplayNameUppercase = "KNOWTE";
        public static string Copyright = "Copyright Digimezzo © 2013 - " + DateTime.Now.Year;
        public static string PayPalLink = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MQALEWTEZ7HX8";
        public static string PreviewLabel = "Preview";

		public static string ReleaseLabel = "Release";
		public static ExternalComponent[] Components = {
            new ExternalComponent {
                Name = "DotNetZip",
                Link = "http://dotnetzip.codeplex.com"
            },
            new ExternalComponent {
                Name = "Prism",
                Link = "http://compositewpf.codeplex.com/"
            },
            new ExternalComponent {
                Name = "Windows Installer XML Toolset (WiX)",
                Link = "http://wix.codeplex.com"
            },
            new ExternalComponent {
                Name = "Unity.WCF",
                Link = "https://unitywcf.codeplex.com/"
            }
        };
    }
}