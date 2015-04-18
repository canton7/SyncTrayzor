using SyncTrayzor.SyncThing.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class ConnectionStatsChangedEventArgs : EventArgs
    {
        public SyncThingConnectionStats TotalConnectionStats { get; private set; }

        public ConnectionStatsChangedEventArgs(SyncThingConnectionStats totalConnectionStats)
        {
            this.TotalConnectionStats = totalConnectionStats;
        }
    }

    public interface ISyncThingConnectionsWatcher : ISyncThingPoller
    {
        event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;
    }

    public class SyncThingConnectionsWatcher : SyncThingPoller, ISyncThingConnectionsWatcher
    {
        private readonly ISyncThingApiClient apiClient;
        
        private DateTime lastPollCompletion;
        private Connections prevConnections;
        private bool haveNotifiedOfNoChange;

        public event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;

        public SyncThingConnectionsWatcher(ISyncThingApiClient apiClient)
            : base(TimeSpan.FromSeconds(10))
        {
            this.apiClient = apiClient;
        }

        protected override async Task PollAsync(CancellationToken cancellationToken)
        {
            var connections = await this.apiClient.FetchConnectionsAsync();

            // We can be stopped in the time it takes this to complete
            cancellationToken.ThrowIfCancellationRequested();
            
            var elapsed = DateTime.UtcNow - this.lastPollCompletion;
            this.lastPollCompletion = DateTime.UtcNow;

            if (this.prevConnections != null)
            {
                // Just do the total for now
                var total = connections.Total;
                var prevTotal = this.prevConnections.Total;

                if (total.InBytesTotal != prevTotal.InBytesTotal || total.OutBytesTotal != prevTotal.OutBytesTotal)
                {
                    this.haveNotifiedOfNoChange = false;

                    double inBytesPerSecond = (total.InBytesTotal - prevTotal.InBytesTotal) / elapsed.TotalSeconds;
                    double outBytesPerSecond = (total.OutBytesTotal - prevTotal.OutBytesTotal) / elapsed.TotalSeconds;

                    var totalStats = new SyncThingConnectionStats(total.InBytesTotal, total.OutBytesTotal, inBytesPerSecond, outBytesPerSecond);
                    this.OnTotalConnectionStatsChanged(totalStats);
                }
                else if (!this.haveNotifiedOfNoChange)
                {
                    this.haveNotifiedOfNoChange = true;

                    var totalStats = new SyncThingConnectionStats(total.InBytesTotal, total.OutBytesTotal, 0, 0);
                    this.OnTotalConnectionStatsChanged(totalStats);
                }
            }
            this.prevConnections = connections;
        }

        private void OnTotalConnectionStatsChanged(SyncThingConnectionStats connectionStats)
        {
            var handler = this.TotalConnectionStatsChanged;
            if (handler != null)
                handler(this, new ConnectionStatsChangedEventArgs(connectionStats));
        }
    }
}
