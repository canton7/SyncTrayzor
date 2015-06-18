using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EventType
    {
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
    }
}
