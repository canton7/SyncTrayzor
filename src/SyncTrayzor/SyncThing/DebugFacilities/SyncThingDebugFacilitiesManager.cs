using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncTrayzor.Services.Config;
using SyncTrayzor.SyncThing.ApiClient;

namespace SyncTrayzor.SyncThing.DebugFacilities
{
    public interface ISyncThingDebugFacilitiesManager
    {
        IReadOnlyList<DebugFacility> DebugFacilities { get; }

        void SetEnabledDebugFacilities(IEnumerable<string> enabledDebugFacilities);
    }

    public class SyncThingDebugFacilitiesManager : ISyncThingDebugFacilitiesManager
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

        private readonly SynchronizedTransientWrapper<ISyncThingApiClient> apiClient;

        private bool syncthingSupports;
        private DebugFacilitiesSettings fetchedDebugFacilitySettings;
        private List<string> enabledDebugFacilities = new List<string>();

        public IReadOnlyList<DebugFacility> DebugFacilities { get; private set; }

        public SyncThingDebugFacilitiesManager(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient)
        {
            this.apiClient = apiClient;
            this.syncthingSupports = false;
        }

        public async Task LoadAsync(Version syncthingVersion)
        {
            if (syncthingVersion.Minor < 12)
            {
                this.syncthingSupports = false;
                this.fetchedDebugFacilitySettings = null;
            }
            else
            {
                this.syncthingSupports = true;
                this.fetchedDebugFacilitySettings = await this.apiClient.Value.FetchDebugFacilitiesAsync();
            }

            this.UpdateDebugFacilities();
        }

        private void UpdateDebugFacilities()
        {
            if (this.syncthingSupports)
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

            if (!this.syncthingSupports)
                return;

            var enabled = this.DebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name).ToList();
            var disabled = this.DebugFacilities.Where(x => !x.IsEnabled).Select(x => x.Name).ToList();

            // TODO: Skip the update if there's nothing to change...
            var apiClient = this.apiClient.Value;
            if (apiClient != null)
                await apiClient.SetDebugFacilitiesAsync(enabled, disabled);
        }
    }
}
