namespace SyncTrayzor.Syncthing.ApiClient
{
    public interface IEventVisitor
    {
        void Accept(GenericEvent evt);
        void Accept(RemoteIndexUpdatedEvent evt);
        void Accept(LocalIndexUpdatedEvent evt);
        void Accept(StateChangedEvent evt);
        void Accept(ItemStartedEvent evt);
        void Accept(ItemFinishedEvent evt);
        void Accept(StartupCompleteEvent evt);
        void Accept(DeviceConnectedEvent evt);
        void Accept(DeviceDisconnectedEvent evt);
        void Accept(DownloadProgressEvent evt);
        void Accept(ConfigSavedEvent evt);
        void Accept(FolderSummaryEvent evt);
        void Accept(FolderErrorsEvent evt);
        void Accept(DevicePausedEvent evt);
        void Accept(DeviceResumedEvent evt);
        void Accept(DeviceRejectedEvent evt);
        void Accept(FolderRejectedEvent evt);
    }
}
