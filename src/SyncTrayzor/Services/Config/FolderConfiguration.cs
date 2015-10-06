namespace SyncTrayzor.Services.Config
{
    public class FolderConfiguration
    {
        public string ID { get; set; }
        public bool IsWatched { get; set; }
        public bool NotificationsEnabled { get; set; }

        public FolderConfiguration()
        {
        }

        public FolderConfiguration(string id, bool isWatched, bool notificationsEnabled)
        {
            this.ID = id;
            this.IsWatched = isWatched;
            this.NotificationsEnabled = notificationsEnabled;
        }

        public FolderConfiguration(FolderConfiguration other)
        {
            this.ID = other.ID;
            this.IsWatched = other.IsWatched;
            this.NotificationsEnabled = other.NotificationsEnabled;
        }

        public override string ToString()
        {
            return $"<Folder ID={this.ID} IsWatched={this.IsWatched} NotificationsEnabled={this.NotificationsEnabled}>";
        }
    }
}
