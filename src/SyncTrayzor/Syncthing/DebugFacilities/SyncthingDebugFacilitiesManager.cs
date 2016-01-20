using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing.DebugFacilities
{
    public interface ISyncthingDebugFacilitiesManager
    {
        bool SupportsRestartlessUpdate { get; }
        IReadOnlyList<DebugFacility> DebugFacilities { get; }

        void SetEnabledDebugFacilities(IEnumerable<string> enabledDebugFacilities);
    }

    public class SyncthingDebugFacilitiesManager : ISyncthingDebugFacilitiesManager
    {
        private static readonly Dictionary<string, string> legacyFacilities = new Dictionary<string, string>()
        {
            { "beacon", "the beacon package" },
            { "discover", "the discover package" },
            { "events", "the events package" },
            { "files", "the files package" },
            { "http", "the main package; HTTP requests" },
            { "locks", "the locks package; trace long held locks" },
            { "net", "the main package; connections & network events" },
            { "model", "the model package" },
            { "scanner", "the scanner package" },
            { "stats", "the stats package" },
            { "suture", "the suture package; service management" },
            { "upnp", "the upnp package" },
            { "xdr", "the xdr package" }
        };

        private readonly SynchronizedTransientWrapper<ISyncthingApiClient> apiClient;
        private readonly ISyncthingCapabilities capabilities;

        private DebugFacilitiesSettings fetchedDebugFacilitySettings;
        private List<string> enabledDebugFacilities = new List<string>();

        public bool SupportsRestartlessUpdate { get; private set; }
        public IReadOnlyList<DebugFacility> DebugFacilities { get; private set; }

        public SyncthingDebugFacilitiesManager(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient, ISyncthingCapabilities capabilities)
        {
            this.apiClient = apiClient;
            this.capabilities = capabilities;

            this.SupportsRestartlessUpdate = false;
            this.DebugFacilities = new List<DebugFacility>();
        }

        public async Task LoadAsync()
        {
            if (this.capabilities.SupportsDebugFacilities)
            {
                this.SupportsRestartlessUpdate = false;
                this.fetchedDebugFacilitySettings = null;
            }
            else
            {
                this.SupportsRestartlessUpdate = true;
                this.fetchedDebugFacilitySettings = await this.apiClient.Value.FetchDebugFacilitiesAsync();
            }

            this.UpdateDebugFacilities();
        }

        private void UpdateDebugFacilities()
        {
            if (this.SupportsRestartlessUpdate)
                this.DebugFacilities = this.fetchedDebugFacilitySettings.Facilities.Select(kvp => new DebugFacility(kvp.Key, kvp.Value, this.enabledDebugFacilities.Contains(kvp.Key))).ToList().AsReadOnly();
            else
                this.DebugFacilities = legacyFacilities.Select(kvp => new DebugFacility(kvp.Key, kvp.Value, this.enabledDebugFacilities.Contains(kvp.Key))).ToList().AsReadOnly();
        }

        public async void SetEnabledDebugFacilities(IEnumerable<string> enabledDebugFacilities)
        {
            var enabledDebugFacilitiesList = enabledDebugFacilities?.ToList() ?? new List<string>();

            if (new HashSet<string>(this.enabledDebugFacilities).SetEquals(enabledDebugFacilitiesList))
                return;

            this.enabledDebugFacilities = enabledDebugFacilitiesList;
            this.UpdateDebugFacilities();

            if (!this.SupportsRestartlessUpdate)
                return;

            var enabled = this.DebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name).ToList();
            var disabled = this.DebugFacilities.Where(x => !x.IsEnabled).Select(x => x.Name).ToList();

            var apiClient = this.apiClient.Value;
            if (apiClient != null)
                await apiClient.SetDebugFacilitiesAsync(enabled, disabled);
        }
    }
}
