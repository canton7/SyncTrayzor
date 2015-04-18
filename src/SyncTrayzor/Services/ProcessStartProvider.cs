using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IProcessStartProvider
    {
        void Start(string filename);
        void Start(string filename, string arguments);
        void StartDetached(string filename);
        void StartDetached(string filename, string arguments);
        void StartElevatedDetached(string filename, string arguments, string launchAfterFinished = null);
    }

    public class ProcessStartProvider : IProcessStartProvider
    {
        private static readonly string processRunner = "ProcessRunner.exe";
        private readonly string processRunnerPath;

        public ProcessStartProvider(IAssemblyProvider assemblyProvider)
        {
            this.processRunnerPath = Path.Combine(Path.GetDirectoryName(assemblyProvider.Location), processRunner);
        }

        public void Start(string filename)
        {
            Process.Start(filename);
        }

        public void Start(string filename, string arguments)
        {
            Process.Start(filename, arguments);
        }

        public void StartDetached(string filename)
        {
            this.StartDetached(filename, null);
        }

        public void StartDetached(string filename, string arguments)
        {
            if (arguments == null)
                arguments = String.Empty;

            var startInfo = new ProcessStartInfo()
            {
                FileName = processRunnerPath,
                Arguments = String.Format("--shell -- \"{0}\" {1}", filename, arguments),
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }

        public void StartElevatedDetached(string filename, string arguments, string launchAfterFinished = null)
        {
            if (arguments == null)
                arguments = String.Empty;

            var launch = launchAfterFinished == null ? null : String.Format("--launch=\"{0}\"", launchAfterFinished.Replace("\"", "\\\""));

            var startInfo = new ProcessStartInfo()
            {
                FileName = processRunnerPath,
                Arguments = String.Format("--nowindow -- \"{0}\" --runas {1} -- \"{2}\" {3}", processRunnerPath, launch, filename, arguments),
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }
    }
}
