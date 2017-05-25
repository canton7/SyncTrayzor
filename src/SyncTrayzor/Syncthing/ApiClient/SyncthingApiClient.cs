using Newtonsoft.Json;
using NLog;
using RestEase;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class SyncthingApiClient : ISyncthingApiClient
    {
        private static readonly Logger logger = LogManager.GetLogger("SyncTrayzor.Syncthing.ApiClient.SyncthingApiClient");
        private ISyncthingApi api;

        public SyncthingApiClient(Uri baseAddress, string apiKey)
        {
            var httpClient = new HttpClient(new SyncthingHttpClientHandler())
            {
                BaseAddress = baseAddress.NormalizeZeroHost(),
                Timeout = TimeSpan.FromSeconds(70),
            };
            this.api = new RestClient(httpClient)
            {
                JsonSerializerSettings = new JsonSerializerSettings()
                {
                    Converters = { new EventConverter() }
                }
            }.For<ISyncthingApi>();
            this.api.ApiKey = apiKey;
        }

        public Task ShutdownAsync()
        {
            logger.Debug("Requesting API shutdown");
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

        public async Task<Connections> FetchConnectionsAsync(CancellationToken cancellationToken)
        {
            var connections = await this.api.FetchConnectionsAsync(cancellationToken);
            return connections;
        }

        public async Task<SyncthingVersion> FetchVersionAsync()
        {
            var version = await this.api.FetchVersionAsync();
            logger.Debug("Fetched version: {0}", version);
            return version;
        }

        public Task RestartAsync()
        {
            logger.Debug("Restarting Syncthing");
            return this.api.RestartAsync();
        }

        public async Task<FolderStatus> FetchFolderStatusAsync(string folderId, CancellationToken cancellationToken)
        {
            var folderStatus = await this.api.FetchFolderStatusAsync(folderId, cancellationToken);
            logger.Debug("Fetched folder status for folder {0}: {1}", folderId, folderStatus);
            return folderStatus;
        }

        public async Task<DebugFacilitiesSettings> FetchDebugFacilitiesAsync()
        {
            var facilities = await this.api.FetchDebugFacilitiesAsync();
            logger.Debug("Got debug facilities: {0}", facilities);
            return facilities; 
        }

        public Task SetDebugFacilitiesAsync(IEnumerable<string> enable, IEnumerable<string> disable)
        {
            var enabled = String.Join(",", enable);
            var disabled = String.Join(",", disable);
            logger.Debug("Setting trace facilities: enabling {0}; disabling {1}", enabled, disabled);

            return this.api.SetDebugFacilitiesAsync(enabled, disabled);
        }

        public Task PauseDeviceAsync(string deviceId)
        {
            logger.Debug("Pausing device {0}", deviceId);
            return this.api.PauseDeviceAsync(deviceId);
        }

        public Task ResumeDeviceAsync(string deviceId)
        {
            logger.Debug("Resuming device {0}", deviceId);
            return this.api.ResumeDeviceAsync(deviceId);
        }
    }
}
