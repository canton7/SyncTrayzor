using Newtonsoft.Json;

namespace SyncTrayzor.Syncthing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum ItemChangedItemType
    {
        Unknown,

        File,
        Dir,
    }
}
