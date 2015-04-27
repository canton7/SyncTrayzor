﻿using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
                    Name = "Refit",
                    Description = "The automatic type-safe REST library for Xamarin and .NET",
                    Homepage = "http://paulcbetts.github.io/refit",
                    License = "MIT",
                    Notes = "Used for making REST API request to Syncthing and Github",
                    LicenseText = this.LoadLicense("Refit.txt")
                },
                new ThirdPartyComponent()
                {
                    Name = "Json.NET",
                    Description = "Popular high-performance JSON framework for .NET ",
                    Homepage = "http://www.newtonsoft.com/json",
                    License = "MIT",
                    Notes = "JSON deserializer, used in conjunction with Refit",
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
                    Name = "PropertyChanged.Fody",
                    Description = "Injects INotifyPropertyChanged code into properties at compile time",
                    Homepage = "https://github.com/Fody/PropertyChanged",
                    License = "MIT",
                    Notes = "Not distributed with SyncTrayzor, but provides awesome compile-time features",
                    LicenseText = this.LoadLicense("Fody.txt")
                },
            });
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
