using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public class EventConverter : JsonCreationConverter<Event>
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static JsonSerializer eventTypeSerializer = new JsonSerializer();

        private static readonly Dictionary<EventType, Type> eventTypes = new Dictionary<EventType, Type>()
        {
            { EventType.RemoteIndexUpdated, typeof(RemoteIndexUpdatedEvent) },
            { EventType.LocalIndexUpdated, typeof(LocalIndexUpdatedEvent) },
            { EventType.ItemStarted, typeof(ItemStartedEvent) },
            { EventType.ItemFinished, typeof(ItemFinishedEvent) },
            { EventType.StateChanged, typeof(StateChangedEvent) },
            { EventType.StartupComplete, typeof(StartupCompleteEvent) },
            { EventType.DeviceConnected, typeof(DeviceConnectedEvent) },
            { EventType.DeviceDisconnected, typeof(DeviceDisconnectedEvent) },
            { EventType.DownloadProgress, typeof(DownloadProgressEvent) },
            { EventType.ConfigSaved, typeof(ConfigSavedEvent) },
            { EventType.FolderSummary, typeof(FolderSummaryEvent) },
            { EventType.FolderErrors, typeof(FolderErrorsEvent) },
            { EventType.DevicePaused, typeof(DevicePausedEvent) },
            { EventType.DeviceResumed, typeof(DeviceResumedEvent) },
            { EventType.DeviceRejected, typeof(DeviceRejectedEvent) },
            { EventType.FolderRejected, typeof(FolderRejectedEvent) },
        };

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }

        protected override Event Create(Type objectType, JObject jObject)
        {
            // It seems we need to pass in a serializer for it to read the JsonSerializerAttribute on EventType
            var eventType = jObject["type"].ToObject<EventType>(eventTypeSerializer);

            if (eventType == EventType.Unknown)
                logger.Warn($"Unknown event type: {jObject["type"]}");

            if (eventTypes.TryGetValue(eventType, out Type type))
                return (Event)jObject.ToObject(type);
            else
                return jObject.ToObject<GenericEvent>();
        }
    }
}
