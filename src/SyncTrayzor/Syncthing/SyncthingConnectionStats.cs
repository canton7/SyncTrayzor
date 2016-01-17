namespace SyncTrayzor.Syncthing
{
    public class SyncthingConnectionStats
    {
        public long InBytesTotal { get; }
        public long OutBytesTotal { get; }
        public double InBytesPerSecond { get; }
        public double OutBytesPerSecond { get; }

        public SyncthingConnectionStats(long inBytesTotal, long outBytesTotal, double inBytesPerSecond, double outBytesPerSecond)
        {
            this.InBytesTotal = inBytesTotal;
            this.OutBytesTotal = outBytesTotal;
            this.InBytesPerSecond = inBytesPerSecond;
            this.OutBytesPerSecond = outBytesPerSecond;
        }
    }
}
