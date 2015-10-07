using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SyncTrayzor.Pages.Settings
{
    public static class KeyValueStringParser
    {
        public static string FormatItem(string key, string value)
        {
            if (String.IsNullOrEmpty(value))
                return key;

            return String.Format("{0}={1}", key, value.Contains(' ') ? "\"" + value + "\"" : value);
        }

        public static string Format(IEnumerable<KeyValuePair<string, string>> result)
        {
            return String.Join(" ", result.Select(x => FormatItem(x.Key, x.Value)));
        }

        public static bool TryParse(string input, out IEnumerable<KeyValuePair<string, string>> result, bool mustHaveValue = true)
        {
            result = new List<KeyValuePair<string, string>>();

            if (String.IsNullOrWhiteSpace(input))
                return true;

            // http://stackoverflow.com/a/4780801/1086121
            var parts = Regex.Split(input.Trim(), "(?<=^[^\"]+(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            var finalResult = new List<KeyValuePair<string, string>>();
            foreach (var part in parts)
            {
                var subParts = part.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length > 2)
                    return false;
                if (subParts.Length < (mustHaveValue ? 2 : 1))
                    return false;

                string key = subParts[0];
                string value = subParts.Length > 1 ? subParts[1] : String.Empty;

                if (key.Contains('"'))
                    return false;

                // This catches the case that the test below doesn't
                if (value == "\"")
                    return false;

                if (value.StartsWith("\"") != value.EndsWith("\""))
                    return false;

                finalResult.Add(new KeyValuePair<string, string>(key, value.Trim('"')));
            }

            result = finalResult;
            return true;
        }
    }
}
