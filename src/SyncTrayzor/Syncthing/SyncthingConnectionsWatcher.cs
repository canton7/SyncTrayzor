using SyncTrayzor.Syncthing.ApiClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing
{
    public class ConnectionStatsChangedEventArgs : EventArgs
    {
        public SyncthingConnectionStats TotalConnectionStats { get; }

        public ConnectionStatsChangedEventArgs(SyncthingConnectionStats totalConnectionStats)
        {
            this.TotalConnectionStats = totalConnectionStats;
        }
    }

    public interface ISyncthingConnectionsWatcher : ISyncthingPoller
    {
        event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;
    }

    public class SyncthingConnectionsWatcher : SyncthingPoller, ISyncthingConnectionsWatcher
    {
        private readonly SynchronizedTransientWrapper<ISyncthingApiClient> apiClientWrapper;
        private ISyncthingApiClient apiClient;
        
        private DateTime lastPollCompletion;
        private Connections prevConnections;

        public event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;

        public SyncthingConnectionsWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient)
            : base(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        {
            this.apiClientWrapper = apiClient;
        }

        protected override void OnStart()
        {
            this.apiClient = this.apiClientWrapper.Value;
            this.prevConnections = null;
        }

        protected override void OnStop()
        {
            this.apiClient = null;

            // Send an update with zero transfer rate, since that's what we're now doing
            this.Update(this.prevConnections);
        }

        protected override async Task PollAsync(CancellationToken cancellationToken)
        {
            var connections = await this.apiClient.FetchConnectionsAsync(cancellationToken);

            // We can be stopped in the time it takes this to complete
            cancellationToken.ThrowIfCancellationRequested();

            this.Update(connections);
        }

        private void Update(Connections connections)
        {
            var elapsed = DateTime.UtcNow - this.lastPollCompletion;
            this.lastPollCompletion = DateTime.UtcNow;

            if (this.prevConnections != null)
            {
                // Just do the total for now
                var total = connections.Total;
                var prevTotal = this.prevConnections.Total;

                double inBytesPerSecond = (total.InBytesTotal - prevTotal.InBytesTotal) / elapsed.TotalSeconds;
                double outBytesPerSecond = (total.OutBytesTotal - prevTotal.OutBytesTotal) / elapsed.TotalSeconds;

                var totalStats = new SyncthingConnectionStats(total.InBytesTotal, total.OutBytesTotal, inBytesPerSecond, outBytesPerSecond);
                this.OnTotalConnectionStatsChanged(totalStats);
            }
            this.prevConnections = connections;
        }

        private void OnTotalConnectionStatsChanged(SyncthingConnectionStats connectionStats)
        {
            this.TotalConnectionStatsChanged?.Invoke(this, new ConnectionStatsChangedEventArgs(connectionStats));
        }
    }
}
