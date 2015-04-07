using Newtonsoft.Json;
using NLog;
using Refit;
using SyncTrayzor.SyncThing.Api;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
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
    }

    public class SyncThingApiClient : ISyncThingApiClient
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private ISyncThingApi api;

        public SyncThingApiClient(Uri baseAddress, string apiKey)
        {
            var httpClient = new HttpClient(new AuthenticatedHttpClientHandler(apiKey))
            {
                BaseAddress = baseAddress.NormalizeZeroHost(),
                Timeout = TimeSpan.FromSeconds(70),
            };
            this.api = RestService.For<ISyncThingApi>(httpClient, new RefitSettings()
            {
                JsonSerializerSettings = new JsonSerializerSettings()
                {
                    Converters = { new EventConverter() }
                },
            });
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

        private class AuthenticatedHttpClientHandler : WebRequestHandler
        {
            private readonly string apiKey;

            public AuthenticatedHttpClientHandler(string apiKey)
            {
                this.apiKey = apiKey;
                // We expect Syncthing to return invalid certs
                this.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                request.Headers.Add("X-API-Key", this.apiKey);
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
