using Stylet;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
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
        private readonly IProcessStartProvider processStartProvider;

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

        public UnhandledExceptionViewModel(IApplicationPathsProvider applicationPathsProvider, IProcessStartProvider processStartProvider)
        {
            this.processStartProvider = processStartProvider;

            this.IssuesUrl = Settings.Default.IssuesUrl;
            this.LogFilePath = applicationPathsProvider.LogFilePath;
        }

        public void ShowIssues()
        {
            this.processStartProvider.StartDetached(this.IssuesUrl);
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
