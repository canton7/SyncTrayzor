using System;
using System.Text.RegularExpressions;

namespace SyncTrayzor.Syncthing
{
    public class SyncthingVersionInformation
    {
        private static readonly Regex versionParseRegex = new Regex(@"\d+\.\d+\.\d+");

        public string ShortVersion { get; }
        public string LongVersion { get; }
        public Version ParsedVersion { get; }

        public SyncthingVersionInformation(string shortVersion, string longVersion)
        {
            this.ShortVersion = shortVersion;
            this.LongVersion = longVersion;

            var parsedVersion = new Version(0, 0, 0);

            var match = versionParseRegex.Match(shortVersion);
            if (match.Success)
                Version.TryParse(match.Value, out parsedVersion);

            this.ParsedVersion = parsedVersion;
        }
    }
}
