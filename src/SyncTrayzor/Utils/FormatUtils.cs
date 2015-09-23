using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using System;

namespace SyncTrayzor.Utils
{
    public static class FormatUtils
    {
        private static readonly string[] sizes = { "B", "KiB", "MiB", "GiB" };

        public static string BytesToHuman(double bytes, int decimalPlaces = 0)
        {
            // http://stackoverflow.com/a/281679/1086121
            int order = 0;
            while (bytes >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                bytes = bytes / 1024;
            }
            var placesFmtString = new String('0', decimalPlaces);
            return String.Format("{0:0." + placesFmtString + "}{1}", bytes, sizes[order]);
        }

        public static string TimeSpanToTimeAgo(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays > 365)
            {
                int years = (int)Math.Ceiling((float)timeSpan.Days / 365);
                return Localizer.F(Resources.TimeAgo_Years, years);
            }

            if (timeSpan.TotalDays > 1.0)
                return Localizer.F(Resources.TimeAgo_Days, (int)timeSpan.TotalDays);

            if (timeSpan.TotalHours > 1.0)
                return Localizer.F(Resources.TimeAgo_Hours, (int)timeSpan.TotalHours);

            if (timeSpan.TotalMinutes > 1.0)
                return Localizer.F(Resources.TimeAgo_Minutes, (int)timeSpan.TotalMinutes);

            return Resources.TimeAgo_JustNow;
        }
    }
}