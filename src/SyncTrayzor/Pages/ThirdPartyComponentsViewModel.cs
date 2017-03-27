using Stylet;
using SyncTrayzor.Services;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SyncTrayzor.Pages
{
    public class ThirdPartyComponentsViewModel : Conductor<ThirdPartyComponent>.Collection.OneActive
    {
        private readonly IProcessStartProvider processStartProvider;

        public ThirdPartyComponentsViewModel(IProcessStartProvider processStartProvider)
        {
            this.processStartProvider = processStartProvider;

            this.Items.AddRange(new[]
            {
                // I'm in two minds as to whether to localize these or not...
                new ThirdPartyComponent()
                {
                    Name = "Syncthing",
                    Description = "Open Source Continuous File Synchronization",
                    Homepage = "http://syncthing.net",
                    License = "MPLv2",
                    Notes = "SyncTrayzor hosts Syncthing",
                    LicenseText = this.LoadLicense("Syncthing.txt"),
                },
                new ThirdPartyComponent()
                {
                    Name = "Stylet",
                    Description = "Minimal MVVM framework",
                    Homepage = "https://github.com/canton7/Stylet",
                    License = "MIT",
                    Notes = "Used to build the UI",
                    LicenseText = this.LoadLicense("Stylet.txt"),
                },
                new ThirdPartyComponent()
                {
                    Name = "RestEase",
                    Description = "Easy-to-use typesafe REST API client library, which is simple and customisable",
                    Homepage = "https://github.com/canton7/RestEase",
                    License = "MIT",
                    Notes = "Used for making REST API request to Syncthing and Github",
                    LicenseText = this.LoadLicense("RestEase.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "NLog",
                    Description = "NLog is a free logging platform for .NET, Xamarin, Silverlight and Windows Phone with rich log routing and management capabilities",
                    Homepage = "http://nlog-project.org/",
                    License = "BSD 3-clause",
                    Notes = "Used for logging",
                    LicenseText = this.LoadLicense("NLog.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "CEF",
                    Description = "Simple framework for embedding Chromium-based browsers in other applications",
                    Homepage = "https://code.google.com/p/chromiumembedded",
                    License = "Modified BSD License",
                    Notes = "Browser component - used to display Syncthing UI",
                    LicenseText = this.LoadLicense("CEF.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "CefSharp",
                    Description = ".NET (WPF and Windows Forms) bindings for the Chromium Embedded Framework",
                    Homepage = "https://github.com/cefsharp/CefSharp",
                    License = "New BSD License",
                    Notes = "WPF adapter for CEF",
                    LicenseText = this.LoadLicense("CefSharp.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Json.NET",
                    Description = "Popular high-performance JSON framework for .NET ",
                    Homepage = "http://www.newtonsoft.com/json",
                    License = "MIT",
                    Notes = "JSON deserializer, used in conjunction with RestEase",
                    LicenseText = this.LoadLicense("Json.NET.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "NotifyIcon WPF",
                    Description = "An implementation of a NotifyIcon (aka system tray icon or taskbar icon) for the WPF platform",
                    Homepage = "http://www.hardcodet.net/wpf-notifyicon",
                    License = "The Code Project Open License (CPOL) 1.02",
                    Notes = "Provides the tray icon",
                    LicenseText = this.LoadLicense("NotifyIcon.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Fluent Validation",
                    Description = "A small validation library for .NET that uses a fluent interface and lambda expressions for building validation rules for your business objects",
                    Homepage = "https://fluentvalidation.codeplex.com",
                    License = "Apache License 2.0",
                    Notes = "Provides validation for user inputs",
                    LicenseText = this.LoadLicense("FluentValidation.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "LongPath",
                    Description = "Drop-in library to support long paths in .NET",
                    Homepage = "https://github.com/peteraritchie/LongPath",
                    License = "LGPLv3",
                    Notes = "Required for long path support (since .NET sucks here)",
                    LicenseText = this.LoadLicense("LongPath.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "SmartFormat.NET",
                    Description = "An extensible .NET replacement for String.Format",
                    Homepage = "https://github.com/scottrippey/SmartFormat.NET",
                    License = "MIT",
                    Notes = "Handles fomatting in language strings",
                    LicenseText = this.LoadLicense("SmartFormat.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "BouncyCastle",
                    Description = "BouncyCastle.Crypto is a cryptography API",
                    Homepage = "http://www.bouncycastle.org/csharp/",
                    License = "MIT",
                    Notes = "Used to sign and verify sha1sum / sha512 files",
                    LicenseText = this.LoadLicense("BouncyCastle.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Mono.Options",
                    Description = "A Getopt::Long-inspired option parsing library for C#",
                    Homepage = "http://tirania.org/blog/archive/2008/Oct-14.html",
                    License = "MIT",
                    Notes = "Used to parse command-line options",
                    LicenseText = this.LoadLicense("Mono.Options.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "ListView Layout Manager",
                    Description = "WPF: Customizing ListView/GridView Column-Layout",
                    Homepage = "http://www.codeproject.com/Articles/25058/ListView-Layout-Manager",
                    License = "The Code Project Open License (CPOL) 1.02",
                    Notes = "Used for layout",
                    LicenseText = this.LoadLicense("ListViewLayoutManager.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "PropertyChanged.Fody",
                    Description = "Injects INotifyPropertyChanged code into properties at compile time",
                    Homepage = "https://github.com/Fody/PropertyChanged",
                    License = "MIT",
                    Notes = "Not distributed with SyncTrayzor, but provides awesome compile-time features",
                    LicenseText = this.LoadLicense("Fody.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Windows API Code Pack - Shell",
                    Description = "Shell library for Windows API Code Pack",
                    Homepage = "https://github.com/aybe/Windows-API-Code-Pack-1.1",
                    License = "Microsoft Software License",
                    Notes = "Provides the 'Open Folder' dialog",
                    LicenseText = this.LoadLicense("WindowsAPICodePack.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Reactive Extensions",
                    Description = "The Reactive Extensions (Rx) is a library for composing asynchronous and event-based programs using observable sequences and LINQ-style query operators",
                    Homepage = "http://rx.codeplex.com/",
                    License = "Microsoft Software License",
                    Notes = "Used internally for some background operations",
                    LicenseText = this.LoadLicense("Rx.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "OxyPlot",
                    Description = "OxyPlot is a cross-platform plotting library for .NET",
                    Homepage = "http://www.oxyplot.org",
                    License = "MIT",
                    Notes = "Use to draw the network usage graph in the tray popup",
                    LicenseText = this.LoadLicense("OxyPlot.txt")
                }
            }.OrderBy(x => x.Name));
        }

        private string LoadLicense(string licenseName)
        {
           using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("SyncTrayzor.Resources.Licenses." + licenseName)))
           {
               return sr.ReadToEnd();
           }
        }

        public void ViewHomepage()
        {
            this.processStartProvider.StartDetached(this.ActiveItem.Homepage);
        }
    }

    public class ThirdPartyComponent    
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Homepage { get; set; }
        public string Notes { get; set; }
        public string License { get; set; }
        public string LicenseText { get; set; }
    }
}
