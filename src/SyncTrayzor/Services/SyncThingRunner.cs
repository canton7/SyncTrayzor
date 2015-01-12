using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public enum SyncThingState {  Started, Stopped }

    public class SyncThingStateChangedEventArgs : EventArgs
    {
        public SyncThingState State { get; private set; }

        public SyncThingStateChangedEventArgs(SyncThingState state)
        {
            this.State = state;
        }
    }

    public interface ISyncThingRunner : IDisposable
    {
        string ExecutablePath { get; set; }
        IObservable<string> LogMessages { get; }
        SyncThingState State { get; }

        event EventHandler<SyncThingStateChangedEventArgs> StateChanged;

        void Start();
        void Kill();
    }

    public class SyncThingRunner : ISyncThingRunner
    {
        private static readonly string[] defaultArguments = new[] { "-no-browser" };

        private readonly Subject<string> logMessages = new Subject<string>();
        private Process process;

        public string ExecutablePath { get; set; }
        public SyncThingState State { get; private set; }
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public IObservable<string> LogMessages
        {
            get { return this.logMessages; }
        }

        public SyncThingRunner()
        {
            this.State = SyncThingState.Stopped;
        }

        public void Start()
        {
            if (this.State == SyncThingState.Started)
                throw new InvalidOperationException("Already started");

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

            this.process.OutputDataReceived += (o, e) => this.DataReceived(e.Data);
            this.process.BeginOutputReadLine();
            this.process.BeginErrorReadLine();
            this.process.Exited += (o, e) => this.Kill();

            this.SetState(SyncThingState.Started);
        }

        public void Kill()
        {
            if (this.State == SyncThingState.Stopped)
                throw new InvalidOperationException("Already stopped");

            this.KillInternal();
        }

        private void KillInternal()
        {
            if (this.process != null)
            {
                KillProcessAndChildren(this.process.Id);
                this.process = null;
            }

            this.SetState(SyncThingState.Stopped);
        }

        private IEnumerable<string> GenerateArguments()
        {
            return defaultArguments;
        }

        private void SetState(SyncThingState state)
        {
            if (state == this.State)
                return;

            this.State = state;
            var handler = this.StateChanged;
            if (handler != null)
                handler(this, new SyncThingStateChangedEventArgs(state));
        }

        private void DataReceived(string data)
        {
            this.logMessages.OnNext(data);
        }

        public void Dispose()
        {
            this.KillInternal();
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
