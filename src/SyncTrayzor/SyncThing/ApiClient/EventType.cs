using Newtonsoft.Json;

namespace SyncTrayzor.SyncThing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum EventType
    {
        Unknown,

        Starting,
        StartupComplete,
        Ping,
        DeviceDiscovered,
        DeviceConnected,
        DeviceDisconnected,
        RemoteIndexUpdated,
        LocalIndexUpdated,
        ItemStarted,
        ItemFinished,

        // Not quite sure which it's going to be, so play it safe...
        MetadataChanged,
        ItemMetadataChanged,

        StateChanged,
        FolderRejected,
        DeviceRejected,
        ConfigSaved,
        DownloadProgress,
        FolderSummary,
        FolderCompletion,
        FolderErrors,
        RelayStateChanged,
        ExternalPortMappingChanged,
        FolderScanProgress
    }
}
