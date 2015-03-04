using Stylet;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class UnhandledExceptionViewModel : Screen
    {
        public Exception Exception { get; set; }

        public string IssuesUrl { get; private set; }
        public string ErrorMessage
        {
            get { return this.Exception.ToString(); }
        }
        public string LogFilePath { get; private set; }
        public Icon Icon
        {
            get { return SystemIcons.Error; }
        }

        public UnhandledExceptionViewModel(IConfigurationProvider configurationProvider)
        {
            this.DisplayName = "Error!";

            this.IssuesUrl = Settings.Default.IssuesUrl;
            this.LogFilePath = configurationProvider.LogFilePath;
        }

        public void ShowIssues()
        {
            Process.Start(this.IssuesUrl);
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
