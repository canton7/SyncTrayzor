using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class ListExtensions
    {
        public static void Replace<T>(this List<T> list, IEnumerable<T> newValues)
        {
            list.Clear();
            list.AddRange(newValues);
        }
    }
}
