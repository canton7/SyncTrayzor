using System;

namespace SyncTrayzor.Syncthing.TransferHistory
{
    public class FailingTransfer : IEquatable<FailingTransfer>
    {
        public string FolderId { get; }
        public string Path { get; }
        public string Error { get; }

        public FailingTransfer(string folderId, string path, string error)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.Error = error;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FailingTransfer);
        }

        public bool Equals(FailingTransfer other)
        {
            if (ReferenceEquals(other, null))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return this.FolderId == other.FolderId && this.Path == other.Path;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + this.FolderId.GetHashCode();
                hash = hash * 31 + this.Path.GetHashCode();
                return hash;
            }
        }
    }
}
