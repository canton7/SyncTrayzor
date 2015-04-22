using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiClient
    {
        Task ShutdownAsync();
        Task<List<Event>> FetchEventsAsync(int since, int limit);
        Task<List<Event>> FetchEventsAsync(int since);
        Task<Config> FetchConfigAsync();
        Task ScanAsync(string folderId, string subPath);
        Task<SystemInfo> FetchSystemInfoAsync();
        Task<Connections> FetchConnectionsAsync();
        Task<SyncthingVersion> FetchVersionAsync();
        Task<Ignores> FetchIgnoresAsync(string folderId);
        Task RestartAsync();
    }
}
