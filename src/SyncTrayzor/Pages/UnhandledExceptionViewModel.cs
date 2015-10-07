using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using System;
using System.Drawing;
using System.Text;

namespace SyncTrayzor.Pages
{
    public class UnhandledExceptionViewModel : Screen
    {
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAssemblyProvider assemblyProvider;

        public Exception Exception { get; set; }

        public string IssuesUrl { get; }

        public string ErrorMessage
        {
            get { return this.GenerateErrorMessage(); }
        }
        public string LogFilePath { get; }
        public Icon Icon
        {
            get { return SystemIcons.Error; }
        }

        public UnhandledExceptionViewModel(IApplicationPathsProvider applicationPathsProvider, IProcessStartProvider processStartProvider, IAssemblyProvider assemblyProvider)
        {
            this.processStartProvider = processStartProvider;
            this.assemblyProvider = assemblyProvider;

            this.IssuesUrl = Properties.Settings.Default.IssuesUrl;
            this.LogFilePath = applicationPathsProvider.LogFilePath;
        }

        private string GenerateErrorMessage()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Version: {0}; Variant: {1}; Arch: {2}", this.assemblyProvider.FullVersion, Properties.Settings.Default.Variant, this.assemblyProvider.ProcessorArchitecture);
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

        public void Close()
        {
            this.RequestClose(true);
        }
    }
}
