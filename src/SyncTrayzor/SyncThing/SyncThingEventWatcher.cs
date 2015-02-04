using SyncTrayzor.SyncThing.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingEventWatcher
    {
        bool Running { get; set; }
        SyncState SyncState { get; }
        event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        event EventHandler StartupComplete;
    }

    public class SyncThingEventWatcher : ISyncThingEventWatcher, IEventVisitor
    {
        private readonly ISyncThingApiClient apiClient;

        private int lastEventId;

        private bool _running;
        public bool Running
        {
            get { return this._running; }
            set
            {
                this._running = value;
                if (value)
                {
                    this.Start();
                }
            }
        }

        public SyncState SyncState { get; private set; }
        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        public event EventHandler StartupComplete;

        public SyncThingEventWatcher(ISyncThingApiClient apiClient)
        {
            this.apiClient = apiClient;
            this.SyncState = SyncThing.SyncState.Idle;
        }

        private async void Start()
        {
            this.lastEventId = 0;
            try
            {
                while (this._running)
                {
                    bool errored = false;

                    try
                    {
                        List<Event> events;
                        // If we don't know what the latest event ID is (disconnection? new connection?), make sure we find out
                        if (this.lastEventId == 0)
                            events = await this.apiClient.FetchEventsAsync(0, 1);
                        else
                            events = await this.apiClient.FetchEventsAsync(this.lastEventId);

                        foreach (var evt in events)
                        {
                            this.lastEventId = Math.Max(this.lastEventId, evt.Id);
                            System.Diagnostics.Debug.WriteLine(evt);
                            evt.Visit(this);
                        }
                    }
                    catch (HttpRequestException)
                    {
                        errored = true;
                    }
                    catch (IOException)
                    {
                        // Socket forcibly closed. Could be a restart, could be a termination. We'll have to continue and quit if we're stopped
                        // A restart means the lastEventId will be reset
                        this.lastEventId = 0;
                        errored = true;
                    }

                    if (errored)
                        await Task.Delay(1000);
                }
            }
            finally
            {
                this._running = false;
            }
        }

        private void OnSyncStateChanged(SyncState syncState)
        {
            if (syncState == this.SyncState)
                return;

            var oldState = this.SyncState;
            this.SyncState = syncState;

            var handler = this.SyncStateChanged;
            if (handler != null)
                handler(this, new SyncStateChangedEventArgs(oldState, syncState));
        }

        private void OnStartupComplete()
        {
            var handler = this.StartupComplete;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #region IEventVisitor

        public void Accept(GenericEvent evt)
        {
        }

        public void Accept(RemoteIndexUpdatedEvent evt)
        {
        }

        public void Accept(LocalIndexUpdatedEvent evt)
        {
        }

        public void Accept(StateChangedEvent evt)
        {
            var state = evt.Data.To == "syncing" ? SyncState.Syncing : SyncState.Idle;
            this.OnSyncStateChanged(state);
        }

        public void Accept(ItemStartedEvent evt)
        {
        }

        public void Accept(StartupCompleteEvent evt)
        {
            this.OnStartupComplete();
        }

        #endregion
    }
}
