using SyncTrayzor.Properties.Strings;
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

        public static string TimeSpanToTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 365)
            {
                int years = (int)Math.Ceiling((float)timeSpan.Days / 365);
                return String.Format(years == 1 ? "{0} year ago" : "{0} years ago", years);
            }

            if (timeSpan.TotalDays > 1.0)
                return String.Format((int)timeSpan.TotalDays == 1 ? "{0} day ago" : "{0} days ago", (int)timeSpan.TotalDays);

            if (timeSpan.TotalHours > 1.0)
                return String.Format((int)timeSpan.TotalHours == 1 ? "{0} hour ago" : "{0} hours ago", (int)timeSpan.TotalHours);

            if (timeSpan.TotalMinutes > 1.0)
                return String.Format((int)timeSpan.TotalMinutes == 1 ? "{0} minute ago" : "{0} minutes ago", (int)timeSpan.TotalMinutes);

            return "Just now";
        }
    }
}