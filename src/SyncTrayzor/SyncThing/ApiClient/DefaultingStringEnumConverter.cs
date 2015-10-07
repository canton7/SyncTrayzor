using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

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
