using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingConnectionStats
    {
        public int InBytesTotal { get; private set; }
        public int OutBytesTotal { get; private set; }
        public double InBytesPerSecond { get; private set; }
        public double OutBytesPerSecond { get; private set; }

        public SyncThingConnectionStats(int inBytesTotal, int outBytesTotal, double inBytesPerSecond, double outBytesPerSecond)
        {
            this.InBytesTotal = inBytesTotal;
            this.OutBytesTotal = outBytesTotal;
            this.InBytesPerSecond = inBytesPerSecond;
            this.OutBytesPerSecond = outBytesPerSecond;
        }
    }
}
