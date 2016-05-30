using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class StringExtensions
    {
        public static string TrimStart(this string input, string prefix)
        {
            if (input.StartsWith(prefix))
                return input.Substring(prefix.Length);
            return input;
        }
    }
}
