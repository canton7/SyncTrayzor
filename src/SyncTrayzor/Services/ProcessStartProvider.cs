using NLog;
using System;
using System.Diagnostics;
using System.IO;

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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string processRunnerPath;

        public ProcessStartProvider(IAssemblyProvider assemblyProvider)
        {
            this.processRunnerPath = Path.Combine(Path.GetDirectoryName(assemblyProvider.Location), processRunner);
        }

        public void Start(string filename)
        {
            logger.Info("Starting {0}", filename);
            Process.Start(filename);
        }

        public void Start(string filename, string arguments)
        {
            logger.Info("Starting {0} {1}", filename, arguments);
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

            var formattedArguments = $"--shell -- \"{filename}\" {arguments}";

            logger.Info("Starting {0} {1}", processRunnerPath, formattedArguments);
            var startInfo = new ProcessStartInfo()
            {
                FileName = processRunnerPath,
                Arguments = formattedArguments,
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
            var formattedArguments = $"--nowindow -- \"{processRunnerPath}\" --runas {launch} -- \"{filename}\" {arguments}";

            logger.Info("Starting {0} {1}", processRunnerPath, formattedArguments);

            var startInfo = new ProcessStartInfo()
            {
                FileName = processRunnerPath,
                Arguments = formattedArguments,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Process.Start(startInfo);
        }
    }
}
