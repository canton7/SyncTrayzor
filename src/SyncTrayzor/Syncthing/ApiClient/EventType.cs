using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum EventType
    {
        Unknown,

        StartupComplete,
        DeviceConnected,
        DeviceDisconnected,
        RemoteIndexUpdated,
        LocalIndexUpdated,
        ItemStarted,
        ItemFinished,
        StateChanged,
        FolderRejected,
        DeviceRejected,
        ConfigSaved,
        DownloadProgress,
        FolderSummary,
        FolderErrors,
        DevicePaused,
        DeviceResumed,
    }
}
