using Newtonsoft.Json;
using NLog;
using RestEase;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class SyncThingApiClientV0p11 : ISyncThingApiClient
    {
        private static readonly Logger logger = LogManager.GetLogger("SyncTrayzor.SyncThing.ApiClient.SyncThingApiClient");
        private ISyncThingApiV0p11 api;

        public SyncThingApiClientV0p11(Uri baseAddress, string apiKey)
        {
            var httpClient = new HttpClient(new SyncThingHttpClientHandler())
            {
                BaseAddress = baseAddress.NormalizeZeroHost(),
                Timeout = TimeSpan.FromSeconds(70),
            };
            this.api = RestClient.For<ISyncThingApiV0p11>(httpClient, new JsonSerializerSettings()
            {
                Converters = { new EventConverter() }
            });
            this.api.ApiKey = apiKey;
        }

        public Task ShutdownAsync()
        {
            logger.Info("Requesting API shutdown");
            return this.api.ShutdownAsync();
        }

        public Task<List<Event>> FetchEventsAsync(int since, int limit, CancellationToken cancellationToken)
        {
            return this.api.FetchEventsLimitAsync(since, limit, cancellationToken);
        }

        public Task<List<Event>> FetchEventsAsync(int since, CancellationToken cancellationToken)
        {
            return this.api.FetchEventsAsync(since, cancellationToken);
        }

        public async Task<Config> FetchConfigAsync()
        {
            var config = await this.api.FetchConfigAsync();
            logger.Debug("Fetched configuration: {0}", config);
            return config;
        }

        public Task ScanAsync(string folderId, string subPath)
        {
            logger.Debug("Scanning folder: {0} subPath: {1}", folderId, subPath);
            return this.api.ScanAsync(folderId, subPath);
        }

        public async Task<SystemInfo> FetchSystemInfoAsync()
        {
            var systemInfo = await this.api.FetchSystemInfoAsync();
            logger.Debug("Fetched system info: {0}", systemInfo);
            return systemInfo;
        }

        public Task<Connections> FetchConnectionsAsync()
        {
            return this.api.FetchConnectionsAsync();
        }

        public async Task<SyncthingVersion> FetchVersionAsync()
        {
            var version = await this.api.FetchVersionAsync();
            logger.Debug("Fetched version: {0}", version);
            return version;
        }

        public async Task<Ignores> FetchIgnoresAsync(string folderId)
        {
            var ignores = await this.api.FetchIgnoresAsync(folderId);
            logger.Debug("Fetched ignores for folderid {0}: {1}", folderId, ignores);
            return ignores;
        }

        public Task RestartAsync()
        {
            logger.Debug("Restarting Syncthing");
            return this.api.RestartAsync();
        }
    }
}
