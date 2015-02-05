using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public interface ISyncThingApi
    {
        [Get("/rest/events")]
        Task<List<Event>> FetchEventsAsync(int since);

        [Get("/rest/events")]
        Task<List<Event>> FetchEventsLimitAsync(int since, int limit);

        [Get("/rest/config")]
        Task<Config> FetchConfigAsync();

        [Post("/rest/shutdown")]
        Task ShutdownAsync();

        [Post("/rest/scan")]
        Task ScanAsync(string folder, string sub);
    }
}
