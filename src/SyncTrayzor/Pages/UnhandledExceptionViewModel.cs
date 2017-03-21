using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using System;
using System.Drawing;
using System.Text;
using System.IO;

namespace SyncTrayzor.Pages
{
    public class UnhandledExceptionViewModel : Screen
    {
        private readonly IApplicationPathsProvider applicationPathsProvider;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAssemblyProvider assemblyProvider;

        public Exception Exception { get; set; }

        public string IssuesUrl { get; }

        public string ErrorMessage => this.GenerateErrorMessage();
        public Icon Icon => SystemIcons.Error;

        public UnhandledExceptionViewModel(IApplicationPathsProvider applicationPathsProvider, IProcessStartProvider processStartProvider, IAssemblyProvider assemblyProvider)
        {
            this.applicationPathsProvider = applicationPathsProvider;
            this.processStartProvider = processStartProvider;
            this.assemblyProvider = assemblyProvider;

            this.IssuesUrl = AppSettings.Instance.IssuesUrl;
        }

        private string GenerateErrorMessage()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Version: {0}; Variant: {1}; Arch: {2}", this.assemblyProvider.FullVersion, AppSettings.Instance.Variant, this.assemblyProvider.ProcessorArchitecture);
            sb.AppendLine();

            sb.AppendFormat("Path: {0}", this.assemblyProvider.Location);
            sb.AppendLine();

            sb.AppendLine(this.Exception.ToString());

            return sb.ToString();
        }

        public void ShowIssues()
        {
            this.processStartProvider.StartDetached(this.IssuesUrl);
        }

        public void OpenLogFilePath()
        {
            this.processStartProvider.ShowFileInExplorer(Path.Combine(this.applicationPathsProvider.LogFilePath, "SyncTrayzor.log"));
        }

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
