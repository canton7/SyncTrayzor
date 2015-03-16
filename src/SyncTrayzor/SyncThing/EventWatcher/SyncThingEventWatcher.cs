using NLog;
using SyncTrayzor.SyncThing.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public interface ISyncThingEventWatcher : ISyncThingPoller
    {
        event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        event EventHandler StartupComplete;
        event EventHandler<ItemStateChangedEventArgs> ItemStarted;
        event EventHandler<ItemStateChangedEventArgs> ItemFinished;
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
    }

    public class SyncThingEventWatcher : SyncThingPoller, ISyncThingEventWatcher, IEventVisitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly ISyncThingApiClient apiClient;

        private int lastEventId;

        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        public event EventHandler StartupComplete;
        public event EventHandler<ItemStateChangedEventArgs> ItemStarted;
        public event EventHandler<ItemStateChangedEventArgs> ItemFinished;
        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        public event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        public SyncThingEventWatcher(ISyncThingApiClient apiClient)
            : base(TimeSpan.Zero)
        {
            this.apiClient = apiClient;
        }

        protected override void Start()
        {
            this.lastEventId = 0;
            base.Start();
        }

        protected override async Task PollAsync()
        {
            try
            {
                var events = await this.apiClient.FetchEventsAsync(this.lastEventId);

                // We can be aborted in the time it takes to fetch the events
                if (!this.Running)
                    return;

                foreach (var evt in events)
                {
                    this.lastEventId = Math.Max(this.lastEventId, evt.Id);
                    logger.Debug(evt);
                    evt.Visit(this);
                }
            }
            catch (IOException)
            {
                // A restart means the lastEventId will be reset
                this.lastEventId = 0;

                // Need the base method to do the error handling
                throw; 
            }
        }

        private void OnSyncStateChanged(string folderId, FolderSyncState oldState, FolderSyncState syncState)
        {
            var handler = this.SyncStateChanged;
            if (handler != null)
                handler(this, new SyncStateChangedEventArgs(folderId, oldState, syncState));
        }

        private void OnStartupComplete()
        {
            var handler = this.StartupComplete;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnItemStarted(string folder, string item)
        {
            var handler = this.ItemStarted;
            if (handler != null)
                handler(this, new ItemStateChangedEventArgs(folder, item));
        }

        private void OnItemFinished(string folder, string item)
        {
            var handler = this.ItemFinished;
            if (handler != null)
                handler(this, new ItemStateChangedEventArgs(folder, item));
        }

        private void OnDeviceConnected(string deviceId, string address)
        {
            var handler = this.DeviceConnected;
            if (handler != null)
                handler(this, new DeviceConnectedEventArgs(deviceId, address));
        }

        private void OnDeviceDisconnected(string deviceId, string error)
        {
            var handler = this.DeviceDisconnected;
            if (handler != null)
                handler(this, new DeviceDisconnectedEventArgs(deviceId, error));
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
            var oldState = evt.Data.From == "syncing" ? FolderSyncState.Syncing : FolderSyncState.Idle;
            var state = evt.Data.To == "syncing" ? FolderSyncState.Syncing : FolderSyncState.Idle;
            this.OnSyncStateChanged(evt.Data.Folder, oldState, state);
        }

        public void Accept(ItemStartedEvent evt)
        {
            this.OnItemStarted(evt.Data.Folder, evt.Data.Item);
        }

        public void Accept(ItemFinishedEvent evt)
        {
            this.OnItemFinished(evt.Data.Folder, evt.Data.Item);
        }

        public void Accept(StartupCompleteEvent evt)
        {
            this.OnStartupComplete();
        }

        public void Accept(DeviceConnectedEvent evt)
        {
            this.OnDeviceConnected(evt.Data.Id, evt.Data.Address);
        }

        public void Accept(DeviceDisconnectedEvent evt)
        {
            this.OnDeviceDisconnected(evt.Data.Id, evt.Data.Error);
        }

        #endregion
    }
}
