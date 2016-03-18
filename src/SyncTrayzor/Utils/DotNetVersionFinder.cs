using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.Utils
{
    public static class DotNetVersionFinder
    {
        // See https://msdn.microsoft.com/en-us/library/hh925568.aspx#net_d

        private static readonly Dictionary<int, string> versionMapping = new Dictionary<int, string>()
        {
            { 378389, "4.5" },
            { 378675, "4.5.1 on Windows 8.1 or Windows Server 2012 R2" },
            { 378758, "4.5.1 on WIndows 8, Wwndows 7 SPI1, or Windows Vista SP2" },
            { 379893, "4.5.2" },
            { 393295, "4.6 on Windows 10" },
            { 393297, "4.6 on all other OS versions" },
            { 394254, "4.6.1 on Windows 10 November Update systems" },
            { 394271, "4.6.1 on all other OS versions" },
        };

        public static string FindDotNetVersion()
        {
            try
            {
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    return $"{DescriptionForReleaseKey(releaseKey)} ({releaseKey})";
                }
            }
            catch (Exception e)
            {
                return $"Unknown ({e.Message})";
            }
        }

        private static string DescriptionForReleaseKey(int releaseKey)
        {
            string description;
            if (!versionMapping.TryGetValue(releaseKey, out description))
                description = "Unknown";

            return description;
        }
    }
}
