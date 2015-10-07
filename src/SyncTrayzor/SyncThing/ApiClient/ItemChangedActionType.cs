using Newtonsoft.Json;

namespace SyncTrayzor.SyncThing.ApiClient
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
