using Refit;
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
        [Get("/rest/events")]
        Task<List<Event>> FetchEventsAsync(int since);

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsLimitAsync(int since, int limit);

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
    }
}
