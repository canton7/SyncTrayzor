using RestEase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public interface ISyncThingApiV0p10
    {
        [Header("X-API-Key")]
        string ApiKey { get; set; }

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsAsync(int since, CancellationToken cancellationToken);

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsLimitAsync(int since, int limit, CancellationToken cancellationToken);

        [Get("/rest/config")]
        Task<Config> FetchConfigAsync();

        [Post("/rest/shutdown")]
        Task ShutdownAsync();

        [Post("/rest/scan")]
        Task ScanAsync(string folder, string sub);

        [Get("/rest/system")]
        Task<SystemInfo> FetchSystemInfoAsync();

        [Get("/rest/connections")]
        Task<ConnectionsV0p10> FetchConnectionsAsync();

        [Get("/rest/version")]
        Task<SyncthingVersion> FetchVersionAsync();

        [Get("/rest/ignores")]
        Task<Ignores> FetchIgnoresAsync(string folder);

        [Post("/rest/restart")]
        Task RestartAsync();

        [Get("/rest/model")]
        Task<FolderStatus> FetchFolderStatusAsync(string folder, CancellationToken cancellationToken);
    }
}
