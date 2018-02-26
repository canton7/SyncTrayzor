using RestEase;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public interface ISyncthingApi
    {
        [Header("X-API-Key")]
        string ApiKey { get; set; }

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsAsync(int since, CancellationToken cancellationToken);

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsLimitAsync(int since, int limit, CancellationToken cancellationToken);

        [Get("/rest/system/config")]
        Task<Config> FetchConfigAsync();

        [Post("/rest/system/shutdown")]
        Task ShutdownAsync();

        [Post("/rest/db/scan")]
        Task ScanAsync(string folder, string sub);

        [Get("/rest/system/status")]
        Task<SystemInfo> FetchSystemInfoAsync(CancellationToken cancellationToken);

        [Get("/rest/system/connections")]
        Task<Connections> FetchConnectionsAsync(CancellationToken cancellationToken);

        [Get("/rest/system/version")]
        Task<SyncthingVersion> FetchVersionAsync(CancellationToken cancellationToken);

        [Post("/rest/system/restart")]
        Task RestartAsync();

        [Get("/rest/db/status")]
        Task<FolderStatus> FetchFolderStatusAsync(string folder, CancellationToken cancellationToken);

        [Get("/rest/system/debug")]
        Task<DebugFacilitiesSettings> FetchDebugFacilitiesAsync();

        [Post("/rest/system/debug")]
        Task SetDebugFacilitiesAsync(string enable, string disable);

        [Post("/rest/system/pause")]
        Task PauseDeviceAsync(string device);

        [Post("/rest/system/resume")]
        Task ResumeDeviceAsync(string device);
    }
}
