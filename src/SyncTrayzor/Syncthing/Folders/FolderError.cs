using System;

namespace SyncTrayzor.Syncthing.Folders
{
    public class FolderError : IEquatable<FolderError>
    {
        public string Error { get; }
        public string Path { get; }

        public FolderError(string error, string path)
        {
            this.Error = error;
            this.Path = path;
        }

        public bool Equals(FolderError other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            return this.Error == other.Error &&
                this.Path == other.Path;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FolderError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + this.Error.GetHashCode();
                hash = hash * 23 + this.Path.GetHashCode();
                return hash;
            }
        }
    }
}
