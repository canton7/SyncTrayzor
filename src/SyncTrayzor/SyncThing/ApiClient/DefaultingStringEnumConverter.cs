using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class DefaultingStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            catch (JsonSerializationException e)
            {
                if (e.InnerException is ArgumentException)
                {
                    // Failed to convert enum
                    foreach (var element in Enum.GetValues(objectType))
                    {
                        return element;
                    }
                }

                throw; // No values? Highly unusual, but keep the compiler happy
            }
        }
    }
}
