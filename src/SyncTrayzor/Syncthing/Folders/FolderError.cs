namespace SyncTrayzor.Syncthing.Folders
{
    public class FolderError
    {
        public string Error { get; }
        public string Path { get; }

        public FolderError(string error, string path)
        {
            this.Error = error;
            this.Path = path;
        }
    }
}
