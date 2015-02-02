using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingEventWatcher
    {
        bool Running { get; set; }
    }

    public class SyncThingEventWatcher : ISyncThingEventWatcher
    {
        private readonly ISyncThingApiClient apiClient;

        private int lastEventId;

        private bool _running;
        public bool Running
        {
            get { return this._running; }
            set
            {
                this._running = value;
                if (value)
                {
                    this.Start();
                }
            }
        }

        public SyncThingEventWatcher(ISyncThingApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        private async void Start()
        {
            while (this._running)
            {
                bool errored = false;

                try
                {
                    var events = await this.apiClient.FetchEventsAsync(this.lastEventId);
                    foreach (var evt in events)
                    {
                        this.lastEventId = Math.Max(this.lastEventId, evt.Id);
                    }
                }
                catch (HttpRequestException)
                {
                    errored = true;
                }

                if (errored)
                    await Task.Delay(1000);
            }
        }
    }
}
