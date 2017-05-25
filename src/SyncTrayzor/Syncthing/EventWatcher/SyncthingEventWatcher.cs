using NLog;
using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public interface ISyncthingEventWatcher : ISyncthingPoller
    {
        event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        event EventHandler StartupComplete;
        event EventHandler<ItemStartedEventArgs> ItemStarted;
        event EventHandler<ItemFinishedEventArgs> ItemFinished;
        event EventHandler<ItemDownloadProgressChangedEventArgs> ItemDownloadProgressChanged;
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
        event EventHandler<ConfigSavedEventArgs> ConfigSaved;
        event EventHandler<FolderStatusChangedEventArgs> FolderStatusChanged;
        event EventHandler<FolderErrorsChangedEventArgs> FolderErrorsChanged;
        event EventHandler<DevicePausedEventArgs> DevicePaused;
        event EventHandler<DeviceResumedEventArgs> DeviceResumed;
        event EventHandler<DeviceRejectedEventArgs> DeviceRejected;
        event EventHandler<FolderRejectedEventArgs> FolderRejected;
        event EventHandler EventsSkipped;
    }

    public class SyncthingEventWatcher : SyncthingPoller, ISyncthingEventWatcher, IEventVisitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly SynchronizedTransientWrapper<ISyncthingApiClient> apiClientWrapper;
        private readonly TaskFactory taskFactory = new TaskFactory(new LimitedConcurrencyTaskScheduler(1));
        private ISyncthingApiClient apiClient;

        private int lastEventId;

        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;
        public event EventHandler StartupComplete;
        public event EventHandler<ItemStartedEventArgs> ItemStarted;
        public event EventHandler<ItemFinishedEventArgs> ItemFinished;
        public event EventHandler<ItemDownloadProgressChangedEventArgs> ItemDownloadProgressChanged;
        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        public event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;
        public event EventHandler<ConfigSavedEventArgs> ConfigSaved;
        public event EventHandler<FolderStatusChangedEventArgs> FolderStatusChanged;
        public event EventHandler<FolderErrorsChangedEventArgs> FolderErrorsChanged;
        public event EventHandler<DevicePausedEventArgs> DevicePaused;
        public event EventHandler<DeviceResumedEventArgs> DeviceResumed;
        public event EventHandler<DeviceRejectedEventArgs> DeviceRejected;
        public event EventHandler<FolderRejectedEventArgs> FolderRejected;
        public event EventHandler EventsSkipped;

        public SyncthingEventWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient)
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

            if (events.Count > 0)
            {
                // Assume we won't skip events in the middle of a response, just before it
                bool skippedEvents = (this.lastEventId > 0 && (events[0].Id - this.lastEventId) != 1);
                this.lastEventId = events[events.Count - 1].Id;

                this.ProcessEvents(skippedEvents, events, cancellationToken);
            }
        }

        private async void ProcessEvents(bool skippedEvents, List<Event> events, CancellationToken cancellationToken)
        {
            // Shove off the processing to another thread - means we can get back to polling quicker
            // However the task factory we use has a limited concurrency level of 1, so we won't process events out-of-order

            // Await needed to throw unhandled exceptions (if there do happen to be any) back to the dispatcher
            await this.DoWithErrorHandlingAsync(() =>
            {
                return this.taskFactory.StartNew(() =>
                {
                    // We receive events in ascending ID order
                    foreach (var evt in events)
                    {
                        if (evt.IsValid)
                        {
                            logger.Trace(evt);
                            evt.Visit(this);
                        }
                        else
                        {
                            logger.Warn($"Invalid event {evt}. Ignoring...");
                        }
                    }

                    if (skippedEvents)
                    {
                        logger.Debug("Events were skipped");
                        this.OnEventsSkipped();
                    }
                });
            }, cancellationToken);
        }

        private void OnSyncStateChanged(string folderId, string oldState, string syncState)
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

        private void OnConfigSaved(Config config)
        {
            this.ConfigSaved?.Invoke(this, new ConfigSavedEventArgs(config));
        }

        private void OnFolderSummaryChanged(string folderId, FolderStatus summary)
        {
            this.FolderStatusChanged?.Invoke(this, new FolderStatusChangedEventArgs(folderId, summary));
        }

        private void OnFolderErorrsChanged(string folderId, List<FolderErrorData> errors)
        {
            this.FolderErrorsChanged?.Invoke(this, new FolderErrorsChangedEventArgs(folderId, errors));
        }

        private void OnDevicePaused(string deviceId)
        {
            this.DevicePaused?.Invoke(this, new DevicePausedEventArgs(deviceId));
        }

        private void OnDeviceResumed(string deviceId)
        {
            this.DeviceResumed?.Invoke(this, new DeviceResumedEventArgs(deviceId));
        }

        private void OnDeviceRejected(string address, string deviceId)
        {
            this.DeviceRejected?.Invoke(this, new DeviceRejectedEventArgs(deviceId, address));
        }

        private void OnFolderRejected(string deviceId, string folderId)
        {
            this.FolderRejected?.Invoke(this, new FolderRejectedEventArgs(deviceId, folderId));
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
            var oldState = evt.Data.From;
            var state = evt.Data.To;
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

        public void Accept(ConfigSavedEvent evt)
        {
            this.OnConfigSaved(evt.Data);
        }

        public void Accept(FolderSummaryEvent evt)
        {
            this.OnFolderSummaryChanged(evt.Data.Folder, evt.Data.Summary);
        }

        public void Accept(FolderErrorsEvent evt)
        {
            this.OnFolderErorrsChanged(evt.Data.Folder, evt.Data.Errors);
        }

        public void Accept(DevicePausedEvent evt)
        {
            this.OnDevicePaused(evt.Data.DeviceId);
        }

        public void Accept(DeviceResumedEvent evt)
        {
            this.OnDeviceResumed(evt.Data.DeviceId);
        }

        public void Accept(DeviceRejectedEvent evt)
        {
            this.OnDeviceRejected(evt.Data.Address, evt.Data.DeviceId);
        }

        public void Accept(FolderRejectedEvent evt)
        {
            this.OnFolderRejected(evt.Data.DeviceId, evt.Data.FolderId);
        }

        #endregion

    }
}
