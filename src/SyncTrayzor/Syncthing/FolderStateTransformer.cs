using System.Collections.Generic;
using NLog;
using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing
{
    public static class FolderStateTransformer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, FolderSyncState> folderSyncStateLookup = new Dictionary<string, FolderSyncState>()
        {
            { "syncing", FolderSyncState.Syncing },
            { "scanning", FolderSyncState.Scanning },
            { "idle", FolderSyncState.Idle },
            { "error", FolderSyncState.Error },
        };

        public static FolderSyncState SyncStateFromStatus(string state)
        {
            FolderSyncState syncState;
            if (folderSyncStateLookup.TryGetValue(state, out syncState))
                return syncState;

            logger.Warn($"Unknown folder sync state {state}. Defaulting to Idle");

            // Default
            return FolderSyncState.Idle;
        }
    }
}
