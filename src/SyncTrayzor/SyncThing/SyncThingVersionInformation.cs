using System;
using System.Text.RegularExpressions;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingVersionInformation
    {
        private static readonly Regex versionParseRegex = new Regex(@"\d+\.\d+\.\d+");

        public string ShortVersion { get; }
        public string LongVersion { get; }
        public Version ParsedVersion { get; }

        public SyncThingVersionInformation(string shortVersion, string longVersion)
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
