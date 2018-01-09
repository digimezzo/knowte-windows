using Digimezzo.Utilities.Packaging;
using System;

namespace Knowte.Common.Base
{
    public class ProductInformation
    {
        public static string ApplicationGuid = "e3be6998-dbcf-4f99-b2b5-bf046fe680f7";
        public static string ApplicationName = "Knowte";
        public static string ApplicationNameUppercase = "KNOWTE";
        public static string Copyright = "Copyright Digimezzo © 2013 - " + DateTime.Now.Year;
        public static string PayPalLink = "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MQALEWTEZ7HX8";
        public static string PreviewLabel = "Preview";

		public static string ReleaseLabel = "Release";
		public static ExternalComponent[] Components = {
            new ExternalComponent {
                Name = "DotNetZip",
                Description = "A FAST, FREE class library and toolset for manipulating zip files. Use VB, C# or any .NET language to easily create, extract, or update zip files.",
                Url = "http://dotnetzip.codeplex.com"
            },
            new ExternalComponent {
                Name = "Font Awesome",
                Description = "Font Awesome by Dave Gandy.",
                Url = "http://fontawesome.io"
            },
            new ExternalComponent {
                Name = "Prism",
                Description = "Prism provides guidance designed to help you more easily design and build rich, flexible, and easy-to-maintain WPF desktop applications.",
                Url = "http://compositewpf.codeplex.com"
            },
            new ExternalComponent {
                Name = "Sqlite-net",
                Description = "A minimal library to allow .NET and Mono applications to store data in SQLite 3 databases.",
                Url = "https://github.com/praeclarum/sqlite-net"
            },
            new ExternalComponent {
                Name = "Unity.WCF",
                Description = "A library that allows the simple integration of Microsoft's Unity IoC container with WCF.",
                Url = "https://github.com/Uriil/unitywcf"
            },
            new ExternalComponent {
                Name = "Unity",
                Description = "A lightweight extensible dependency injection container with support for constructor, property, and method call injection.",
                Url = "https://unity.codeplex.com"
            },
            new ExternalComponent {
                Name = "WiX",
                Description = "Windows Installer XML Toolset.",
                Url = "http://wix.codeplex.com"
            },
            new ExternalComponent {
                Name = "WPF Native Folder Browser",
                Description = "Use the Windows Vista / Windows 7 Folder Browser Dialog from your WPF projects, without any additional dependencies.",
                Url = "https://wpffolderbrowser.codeplex.com/"
            }
        };
    }
}