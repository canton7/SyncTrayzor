using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class DefaultingStringEnumConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return base.ReadJson(reader, objectType, existingValue, serializer);
            }
            // It seems we can get both...
            catch (ArgumentException)
            {
                return ReadDefaultEnumValue(objectType);
            }
            catch (JsonSerializationException e) when (e.InnerException is ArgumentException)
            {
                return ReadDefaultEnumValue(objectType);
            }
        }

        private static object ReadDefaultEnumValue(Type objectType)
        {
            // Failed to convert enum
            foreach (var element in Enum.GetValues(objectType))
            {
                return element;
            }

            // No values? Highly unusual, but keep the compiler happy
            throw new ArgumentException($"Enum type {objectType.Name} does not have any values");
        }
    }
}
