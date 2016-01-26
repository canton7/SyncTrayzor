using System;

namespace SyncTrayzor.Syncthing
{
    public interface ISyncthingCapabilities
    {
        bool SupportsDebugFacilities { get; }
        bool SupportsDevicePauseResume { get; }
    }

    public class SyncthingCapabilities : ISyncthingCapabilities
    {
        private static readonly Version debugFacilitiesIntroduced = new Version(0, 12, 0);
        private static readonly Version devicePauseResumeIntroduced = new Version(0, 12, 0);

        public Version SyncthingVersion { get; set; } = new Version(0, 0, 0);

        public bool SupportsDebugFacilities => this.SyncthingVersion >= debugFacilitiesIntroduced;
        public bool SupportsDevicePauseResume => this.SyncthingVersion >= devicePauseResumeIntroduced;
    }
}
