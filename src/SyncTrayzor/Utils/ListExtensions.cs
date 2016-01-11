using System.Collections.Generic;

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
