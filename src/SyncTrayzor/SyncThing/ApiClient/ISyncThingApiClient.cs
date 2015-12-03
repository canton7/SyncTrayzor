using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiClient
    {
        Task ShutdownAsync();
        Task<List<Event>> FetchEventsAsync(int since, int limit, CancellationToken cancellationToken);
        Task<List<Event>> FetchEventsAsync(int since, CancellationToken cancellationToken);
        Task<Config> FetchConfigAsync();
        Task ScanAsync(string folderId, string subPath);
        Task<SystemInfo> FetchSystemInfoAsync();
        Task<Connections> FetchConnectionsAsync();
        Task<SyncthingVersion> FetchVersionAsync();
        Task<Ignores> FetchIgnoresAsync(string folderId);
        Task RestartAsync();
        Task<FolderStatus> FetchFolderStatusAsync(string folderId, CancellationToken cancellationToken);
        Task<DebugFacilitiesSettings> FetchDebugFacilitiesAsync();
        Task SetDebugFacilitiesAsync(IEnumerable<string> enable, IEnumerable<string> disable);
    }
}
