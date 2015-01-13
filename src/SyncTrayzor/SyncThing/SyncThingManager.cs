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

        void Start();
        Task StopAsync();
        void Kill();
    }

    public class SyncThingManager : ISyncThingManager
    {
        private readonly ISyncThingProcessRunner processRunner;
        private readonly ISyncThingApiClient apiClient;

        public SyncThingState State { get; private set; }
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;

        public string ExecutablePath
        {
            get { return this.processRunner.ExecutablePath; }
            set { this.processRunner.ExecutablePath = value; }
        }

        public SyncThingManager(ISyncThingProcessRunner processRunner, ISyncThingApiClient apiClient)
        {
            this.processRunner = processRunner;
            this.apiClient = apiClient;

            this.processRunner.ProcessStopped += (o, e) => this.SetState(SyncThingState.Stopped);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            // TEMP
            var address = "http://localhost:4567";
            this.apiClient.BaseAddress = new Uri(address);
            this.processRunner.HostAddress = address;
            var apiKey = "abc123";
            this.apiClient.ApiKey = apiKey;
            this.processRunner.ApiKey = apiKey;
        }

        public void Start()
        {
            this.processRunner.Start();
            this.SetState(SyncThingState.Started);
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

            var handler = this.StateChanged;
            if (handler != null)
                handler(this, new SyncThingStateChangedEventArgs(oldState, state));
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
