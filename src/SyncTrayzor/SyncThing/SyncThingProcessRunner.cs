using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public enum SyncThingExitStatus
    {
        // From https://github.com/syncthing/syncthing/blob/master/cmd/syncthing/main.go#L67
        Success = 0,
        Error = 1,
        NoUpgradeAvailable = 2,
        Restarting = 3,
        Upgrading = 4
    }

    public class ProcessStoppedEventArgs : EventArgs
    {
        public SyncThingExitStatus ExitStatus { get; private set; }

        public ProcessStoppedEventArgs(SyncThingExitStatus exitStatus)
        {
            this.ExitStatus = exitStatus;
        }
    }

    public interface ISyncThingProcessRunner : IDisposable
    {
        string ExecutablePath { get; set; }
        string ApiKey { get; set; }
        string HostAddress { get; set; }
        string CustomHomeDir { get; set; }
        string Traces { get; set; }

        event EventHandler<MessageLoggedEventArgs> MessageLogged;
        event EventHandler<ProcessStoppedEventArgs> ProcessStopped;

        void Start();
        void Kill();
        void KillAllSyncthingProcesses();
    }

    public class SyncThingProcessRunner : ISyncThingProcessRunner
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly string[] defaultArguments = new[] { "-no-browser", "-no-restart" };

        private Process process;

        public string ExecutablePath { get; set; }
        public string ApiKey { get; set; }
        public string HostAddress { get; set; }
        public string CustomHomeDir { get; set; }
        public string Traces { get; set; }

        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<ProcessStoppedEventArgs> ProcessStopped;

        public SyncThingProcessRunner()
        {
        }

        public void Start()
        {
            logger.Info("Starting syncthing: ", this.ExecutablePath);

            if (!File.Exists(this.ExecutablePath))
                throw new Exception(String.Format("Unable to find Syncthing at path {0}", this.ExecutablePath));

            var processStartInfo = new ProcessStartInfo()
            {
                FileName = this.ExecutablePath,
                Arguments = String.Join(" ", this.GenerateArguments()),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            if (!String.IsNullOrWhiteSpace(this.Traces))
            {
                processStartInfo.EnvironmentVariables["STTRACE"] = this.Traces;
            }

            this.process = Process.Start(processStartInfo);

            this.process.EnableRaisingEvents = true;
            this.process.OutputDataReceived += (o, e) => this.DataReceived(e.Data);
            this.process.ErrorDataReceived += (o, e) => this.DataReceived(e.Data);

            this.process.BeginOutputReadLine();
            this.process.BeginErrorReadLine();

            this.process.Exited += (o, e) => this.OnProcessStopped();
        }

        public void Kill()
        {
            logger.Info("Killing Syncthing process");
            this.KillInternal();
        }

        private void KillInternal()
        {
            if (this.process != null)
            {
                this.process.Kill();
            }
        }

        private IEnumerable<string> GenerateArguments()
        {
            var args = new List<string>(defaultArguments)
            {
                String.Format("-gui-apikey=\"{0}\"", this.ApiKey),
                String.Format("-gui-address=\"{0}\"", this.HostAddress)
            };

            if (!String.IsNullOrWhiteSpace(this.CustomHomeDir))
            {
                args.Add(String.Format("-home=\"{0}\"", this.CustomHomeDir));
                args.Add(String.Format("-logfile=\"{0}\"", Path.Combine(this.CustomHomeDir, "syncthing.log")));
            }

            return args;
        }

        private void DataReceived(string data)
        {
            this.OnMessageLogged(data);
        }

        public void Dispose()
        {
            this.KillInternal();
        }

        private void OnProcessStopped()
        {
            SyncThingExitStatus exitStatus = this.process == null ? SyncThingExitStatus.Success : (SyncThingExitStatus)this.process.ExitCode; 
            logger.Info("Syncthing process stopped with exit status {0}", exitStatus);
            if (exitStatus == SyncThingExitStatus.Restarting || exitStatus == SyncThingExitStatus.Upgrading)
            {
                logger.Info("Syncthing process requested restart, so restarting");
                this.Start();
            }
            else
            {
                var handler = this.ProcessStopped;
                if (handler != null)
                    handler(this, new ProcessStoppedEventArgs(exitStatus));
            }
        }

        private void OnMessageLogged(string logMessage)
        {
            logger.Debug(logMessage);
            var handler = this.MessageLogged;
            if (handler != null)
                handler(this, new MessageLoggedEventArgs(logMessage));
        }

        public void KillAllSyncthingProcesses()
        {
            foreach (var process in Process.GetProcessesByName("syncthing"))
            {
                process.Kill();
            }
        }
    }
}
