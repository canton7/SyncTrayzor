using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    [JsonConverter(typeof(DefaultingStringEnumConverter))]
    public enum ItemChangedItemType
    {
        Unknown,

        File,
        Folder,
    }
}
