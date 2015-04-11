using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class FormatUtils
    {
        private static readonly string[] sizes = { "B", "KB", "MB", "GB" };

        public static string BytesToHuman(long bytes)
        {
            // http://stackoverflow.com/a/281679/1086121
            int order = 0;
            while (bytes >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                bytes = bytes / 1024;
            }
            return String.Format("{0:0.#}{1}", bytes, sizes[order]);
        }
    }
}
