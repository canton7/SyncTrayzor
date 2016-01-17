using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum ItemChangedActionType
    {
        Unknown,

        Update,
        Delete,
        Metadata,
    }
}
