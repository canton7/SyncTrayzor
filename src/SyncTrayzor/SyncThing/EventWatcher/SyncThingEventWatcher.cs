using NLog;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public interface ISyncThingEventWatcher : ISyncThingPoller
    {
        event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        event EventHandler StartupComplete;
        event EventHandler<ItemStartedEventArgs> ItemStarted;
        event EventHandler<ItemFinishedEventArgs> ItemFinished;
        event EventHandler<ItemDownloadProgressChangedEventArgs> ItemDownloadProgressChanged;
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
        event EventHandler EventsSkipped;
    }

    public class SyncThingEventWatcher : SyncThingPoller, ISyncThingEventWatcher, IEventVisitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizedTransientWrapper<ISyncThingApiClient> apiClientWrapper;
        private readonly TaskFactory taskFactory = new TaskFactory(new LimitedConcurrencyTaskScheduler(1));
        private ISyncThingApiClient apiClient;

        private int lastEventId;

        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        public event EventHandler StartupComplete;
        public event EventHandler<ItemStartedEventArgs> ItemStarted;
        public event EventHandler<ItemFinishedEventArgs> ItemFinished;
        public event EventHandler<ItemDownloadProgressChangedEventArgs> ItemDownloadProgressChanged;
        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        public event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
        public event EventHandler EventsSkipped;

        public SyncThingEventWatcher(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient)
            : base(TimeSpan.Zero, TimeSpan.FromSeconds(10))
        {
            this.apiClientWrapper = apiClient;
        }

        protected override void OnStart()
        {
            this.lastEventId = 0;
            this.apiClient = this.apiClientWrapper.Value;
        }

        protected override void OnStop()
        {
            this.apiClient = null;
        }

        protected override async Task PollAsync(CancellationToken cancellationToken)
        {
            List<Event> events;
            // If this is the first poll, don't fetch the history
            if (this.lastEventId == 0)
                events = await this.apiClient.FetchEventsAsync(0, 1, cancellationToken);
            else
                events = await this.apiClient.FetchEventsAsync(this.lastEventId, cancellationToken);

            // We can be aborted in the time it takes to fetch the events
            cancellationToken.ThrowIfCancellationRequested();

            logger.Debug("Received {0} events", events.Count);

            // Need to synchronously update the lastEventId
            var oldLastEventId = this.lastEventId;
            this.lastEventId = events[events.Count - 1].Id;

            this.ProcessEvents(oldLastEventId, events, cancellationToken);
        }

        private async void ProcessEvents(int startingEventId, List<Event> events, CancellationToken cancellationToken)
        {
            // Shove off the processing to another thread - means we can get back to polling quicker
            // However the task factory we use has a limited concurrency level of 1, so we won't process events out-of-order

            // Await needed to throw unhandled exceptions (if there do happen to be any) back to the dispatcher
            await this.DoWithErrorHandlingAsync(() =>
            {
                return this.taskFactory.StartNew(() =>
                {
                    bool eventsSkipped = false;

                    // We receive events in ascending ID order
                    foreach (var evt in events)
                    {
                        if (startingEventId > 0 && (evt.Id - startingEventId) != 1)
                            eventsSkipped = true;
                        startingEventId = evt.Id;
                        logger.Debug(evt);
                        evt.Visit(this);
                    }

                    if (eventsSkipped)
                    {
                        logger.Debug("Events were skipped");
                        this.OnEventsSkipped();
                    }
                });
            }, cancellationToken);
        }

        private void OnSyncStateChanged(string folderId, FolderSyncState oldState, FolderSyncState syncState)
        {
            this.SyncStateChanged?.Invoke(this, new SyncStateChangedEventArgs(folderId, oldState, syncState));
        }

        private void OnStartupComplete()
        {
            this.StartupComplete?.Invoke(this, EventArgs.Empty);
        }

        private void OnItemStarted(string folder, string item, ItemChangedActionType action, ItemChangedItemType itemType)
        {
            this.ItemStarted?.Invoke(this, new ItemStartedEventArgs(folder, item, action, itemType));
        }

        private void OnItemFinished(string folder, string item, ItemChangedActionType action, ItemChangedItemType itemType, string error)
        {
            this.ItemFinished?.Invoke(this, new ItemFinishedEventArgs(folder, item, action, itemType, error));
        }

        private void OnItemDownloadProgressChanged(string folder, string item, long bytesDone, long bytesTotal)
        {
            this.ItemDownloadProgressChanged?.Invoke(this, new ItemDownloadProgressChangedEventArgs(folder, item, bytesDone, bytesTotal));
        }

        private void OnDeviceConnected(string deviceId, string address)
        {
            this.DeviceConnected?.Invoke(this, new DeviceConnectedEventArgs(deviceId, address));
        }

        private void OnDeviceDisconnected(string deviceId, string error)
        {
            this.DeviceDisconnected?.Invoke(this, new DeviceDisconnectedEventArgs(deviceId, error));
        }

        private void OnEventsSkipped()
        {
            this.EventsSkipped?.Invoke(this, EventArgs.Empty);
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
            this.OnItemStarted(evt.Data.Folder, evt.Data.Item, evt.Data.Action, evt.Data.Type);
        }

        public void Accept(ItemFinishedEvent evt)
        {
            this.OnItemFinished(evt.Data.Folder, evt.Data.Item, evt.Data.Action, evt.Data.Type, evt.Data.Error);
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

        public void Accept(DownloadProgressEvent evt)
        {
            foreach (var folder in evt.Data)
            {
                foreach (var file in folder.Value)
                {
                    this.OnItemDownloadProgressChanged(folder.Key, file.Key, file.Value.BytesDone, file.Value.BytesTotal);
                }
            }
        }

        #endregion

    }
}
