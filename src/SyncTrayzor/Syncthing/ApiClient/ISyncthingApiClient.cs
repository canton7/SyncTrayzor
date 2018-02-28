using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public interface ISyncthingApiClient
    {
        Task ShutdownAsync();
        Task<List<Event>> FetchEventsAsync(int since, int limit, CancellationToken cancellationToken);
        Task<List<Event>> FetchEventsAsync(int since, CancellationToken cancellationToken);
        Task<Config> FetchConfigAsync();
        Task ScanAsync(string folderId, string subPath);
        Task<SystemInfo> FetchSystemInfoAsync(CancellationToken cancellationToken);
        Task<Connections> FetchConnectionsAsync(CancellationToken cancellationToken);
        Task<SyncthingVersion> FetchVersionAsync(CancellationToken cancellationToken);
        Task RestartAsync();
        Task<FolderStatus> FetchFolderStatusAsync(string folderId, CancellationToken cancellationToken);
        Task<DebugFacilitiesSettings> FetchDebugFacilitiesAsync();
        Task SetDebugFacilitiesAsync(IEnumerable<string> enable, IEnumerable<string> disable);
        Task PauseDeviceAsync(string deviceId);
        Task ResumeDeviceAsync(string deviceId);
    }
}
