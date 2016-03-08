using Microsoft.Win32;
using System;

namespace SyncTrayzor.Utils
{
    public static class DotNetVersionFinder
    {
        // See https://msdn.microsoft.com/en-us/library/hh925568.aspx#net_d

        public static string FindDotNetVersion()
        {
            try
            {
                using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    int releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
                    return $"{CheckFor45DotVersion(releaseKey)} ({releaseKey})";
                }
            }
            catch (Exception e)
            {
                return $"Unknown ({e.Message})";
            }
        }

        private static string CheckFor45DotVersion(int releaseKey)
        {
            if (releaseKey == 394271)
            {
                return "4.6.1 on all other Windows OS versions";
            }
            if (releaseKey == 394254)
            {
                return "4.6.1 on Windows 10";
            }
            if (releaseKey == 393297)
            {
                return "4.6 on all other Windows OS versions";
            }
            if (releaseKey == 393295)
            {
                return "4.6 or later on Windows 10";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2 or later";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1 or later";
            }
            if ((releaseKey >= 378389))
            {
                return "4.5 or later";
            }
            // This line should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }
    }
}
