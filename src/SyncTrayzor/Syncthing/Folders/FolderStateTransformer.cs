using System.Collections.Generic;
using NLog;

namespace SyncTrayzor.Syncthing.Folders
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

        public static FolderSyncState SyncStateFromString(string state)
        {
            if (folderSyncStateLookup.TryGetValue(state, out var syncState))
                return syncState;

            logger.Warn($"Unknown folder sync state {state}. Defaulting to Idle");

            // Default
            return FolderSyncState.Idle;
        }
    }
}
