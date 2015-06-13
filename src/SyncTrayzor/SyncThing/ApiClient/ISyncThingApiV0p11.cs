using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiV0p11
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
        Task<SystemInfo> FetchSystemInfoAsync();

        [Get("/rest/system/connections")]
        Task<Connections> FetchConnectionsAsync();

        [Get("/rest/system/version")]
        Task<SyncthingVersion> FetchVersionAsync();

        [Get("/rest/db/ignores")]
        Task<Ignores> FetchIgnoresAsync(string folder);

        [Post("/rest/system/restart")]
        Task RestartAsync();

        [Get("/rest/db/status")]
        Task<FolderStatus> FetchFolderStatusAsync(string folder, CancellationToken cancellationToken);
    }
}
