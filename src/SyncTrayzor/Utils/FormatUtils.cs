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

        public static string BytesToHuman(double bytes, int decimalPlaces = 0)
        {
            // http://stackoverflow.com/a/281679/1086121
            int order = 0;
            while (bytes >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                bytes = bytes / 1024;
            }
            return Math.Round(bytes, decimalPlaces).ToString() + sizes[order];
        }

        public static string TimeSpanToTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 365)
            {
                int years = (int)Math.Ceiling((float)timeSpan.Days / 365);
                return years == 1 ?
                    Resources.TimeAgo_Years_Singular :
                    String.Format(Resources.TimeAgo_Years_Plural, years);
            }

            if (timeSpan.TotalDays > 1.0)
                return (int)timeSpan.TotalDays == 1 ?
                    Resources.TimeAgo_Days_Singular :
                    String.Format(Resources.TimeAgo_Days_Plural, (int)timeSpan.TotalDays);

            if (timeSpan.TotalHours > 1.0)
                return (int)timeSpan.TotalHours == 1 ?
                    Resources.TimeAgo_Hours_Singular :
                    String.Format(Resources.TimeAgo_Hours_Plural, (int)timeSpan.TotalHours);

            if (timeSpan.TotalMinutes > 1.0)
                return (int)timeSpan.TotalMinutes == 1 ?
                    Resources.TimeAgo_Minutes_Singular :
                    String.Format(Resources.TimeAgo_Minutes_Plural, (int)timeSpan.TotalMinutes);

            return Resources.TimeAgo_JustNow;
        }
    }
}