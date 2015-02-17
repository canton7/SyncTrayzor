using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
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
        StateChanged,
        FolderRejected,
        DeviceRejected,
        ConfigSaved,
        DownloadProgress,
    }
}
