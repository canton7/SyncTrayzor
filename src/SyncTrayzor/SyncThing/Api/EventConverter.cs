using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public class EventConverter : JsonCreationConverter<Event>
    {
        private static readonly Dictionary<EventType, Type> eventTypes = new Dictionary<EventType, Type>()
        {
            { EventType.RemoteIndexUpdated, typeof(RemoteIndexUpdatedEvent) },
            { EventType.LocalIndexUpdated, typeof(LocalIndexUpdatedEvent) },
            { EventType.ItemStarted, typeof(ItemStartedEvent) },
            { EventType.StateChanged, typeof(StateChangedEvent) },
            { EventType.StartupComplete, typeof(StartupCompleteEvent) }
        };

        protected override Event Create(Type objectType, JObject jObject)
        {
            var eventType = jObject["type"].ToObject<EventType>();
            Type type;
            if (eventTypes.TryGetValue(eventType, out type))
                return (Event)jObject.ToObject(type);
            else
                return jObject.ToObject<GenericEvent>();
        }
    }
}
