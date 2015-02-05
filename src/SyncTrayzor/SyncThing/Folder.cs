using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class Folder
    {
        public string FolderId { get; private set; }
        public string Path { get; private set; }

        public Folder(string folderId, string path)
        {
            this.FolderId = folderId;
            this.Path = path;
        }
    }
}
