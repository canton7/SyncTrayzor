using Newtonsoft.Json;

namespace SyncTrayzor.SyncThing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum ItemChangedItemType
    {
        Unknown,

        File,
        Dir,
    }
}
