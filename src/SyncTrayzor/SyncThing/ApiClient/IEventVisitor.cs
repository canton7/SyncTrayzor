namespace SyncTrayzor.SyncThing.ApiClient
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
    }
}
