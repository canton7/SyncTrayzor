using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingManager : IDisposable
    {
        SyncThingState State { get; }
        event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        event EventHandler<MessageLoggedEventArgs> MessageLogged;

        string ExecutablePath { get; set; }
        string Address { get; set; }

        void Start();
        Task StopAsync();
        void Kill();
    }

    public class SyncThingManager : ISyncThingManager
    {
        private readonly ISyncThingProcessRunner processRunner;
        private readonly ISyncThingApiClient apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly string apiKey;

        public SyncThingState State { get; private set; }
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;

        public string ExecutablePath { get; set; }
        public string Address { get; set; }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClient apiClient,
            ISyncThingEventWatcher eventWatcher)
        {
            this.processRunner = processRunner;
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;

            this.processRunner.ProcessStopped += (o, e) => this.SetState(SyncThingState.Stopped);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            this.apiKey = "abc123";
            this.processRunner.ApiKey = apiKey;
        }

        public void Start()
        {
            this.apiClient.SetConnectionDetails(new Uri(this.Address), this.apiKey);
            this.processRunner.HostAddress = this.Address;
            this.processRunner.ExecutablePath = this.ExecutablePath;

            this.processRunner.Start();
            this.SetState(SyncThingState.Running);
        }

        public Task StopAsync()
        {
            this.SetState(SyncThingState.Stopping);
            return this.apiClient.ShutdownAsync();
        }

        public void Kill()
        {
            this.processRunner.Kill();
            this.SetState(SyncThingState.Stopped);
        }

        private void SetState(SyncThingState state)
        {
            if (state == this.State)
                return;

            var oldState = this.State;
            this.State = state;

            this.UpdateEventWatcherState(this.State);

            var handler = this.StateChanged;
            if (handler != null)
                handler(this, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private void UpdateEventWatcherState(SyncThingState state)
        {
            this.eventWatcher.Running = (state == SyncThingState.Running);
        }

        private void OnMessageLogged(string logMessage)
        {
            var handler = this.MessageLogged;
            if (handler != null)
                handler(this, new MessageLoggedEventArgs(logMessage));
        }

        public void Dispose()
        {
            this.processRunner.Dispose();
        }
    }
}
