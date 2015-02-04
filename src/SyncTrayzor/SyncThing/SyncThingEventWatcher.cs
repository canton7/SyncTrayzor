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

        public SyncThingEventWatcher(ISyncThingApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        private async void Start()
        {
            this.lastEventId = 0;

            while (this._running)
            {
                bool errored = false;

                try
                {
                    var events = await this.apiClient.FetchEventsAsync(this.lastEventId);
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
                    // Socket forcibly closed
                    break;
                }

                if (errored)
                    await Task.Delay(1000);
            }

            this._running = false;
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

        #endregion
    }
}
