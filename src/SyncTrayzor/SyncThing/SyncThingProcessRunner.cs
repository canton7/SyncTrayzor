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
    public interface ISyncThingProcessRunner : IDisposable
    {
        string ExecutablePath { get; set; }
        string ApiKey { get; set; }
        string HostAddress { get; set; }

        event EventHandler<MessageLoggedEventArgs> MessageLogged;
        event EventHandler ProcessStopped;

        void Start();
        void Kill();
    }

    public class SyncThingProcessRunner : ISyncThingProcessRunner
    {
        private static readonly string[] defaultArguments = new[] { "-no-browser" };

        private Process process;

        public string ExecutablePath { get; set; }
        public string ApiKey { get; set; }
        public string HostAddress { get; set; }

        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler ProcessStopped;

        public SyncThingProcessRunner()
        {
        }

        public void Start()
        {
            if (!File.Exists(this.ExecutablePath))
                throw new Exception(String.Format("Unable to find SyncThing at path {0}", this.ExecutablePath));

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

            this.process = Process.Start(processStartInfo);

            this.process.EnableRaisingEvents = true;
            this.process.OutputDataReceived += (o, e) => this.DataReceived(e.Data);

            this.process.BeginOutputReadLine();
            this.process.BeginErrorReadLine();

            this.process.Exited += (o, e) => this.OnProcessStopped();
        }

        public void Kill()
        {
            this.KillInternal();
        }

        private void KillInternal()
        {
            if (this.process != null)
            {
                KillProcessAndChildren(this.process.Id);
                this.process = null;
            }
        }

        private IEnumerable<string> GenerateArguments()
        {
            return defaultArguments.Concat(new[]
            {
                String.Format("-gui-apikey=\"{0}\"", this.ApiKey),
                String.Format("-gui-address=\"{0}\"", this.HostAddress)
            });
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
            var handler = this.ProcessStopped;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnMessageLogged(string logMessage)
        {
            var handler = this.MessageLogged;
            if (handler != null)
                handler(this, new MessageLoggedEventArgs(logMessage));
        }

        // http://stackoverflow.com/questions/5901679/kill-process-tree-programatically-in-c-sharp
        private static void KillProcessAndChildren(int pid)
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
