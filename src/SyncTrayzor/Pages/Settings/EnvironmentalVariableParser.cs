using SyncTrayzor.Services.Config;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SyncTrayzor.Pages.Settings
{
    public static class EnvironmentalVariablesParser
    {
        public static string Format(EnvironmentalVariableCollection result)
        {
            return String.Join(" ", result.Select(x => String.Format("{0}={1}", x.Key, x.Value.Contains(' ') ? "\"" + x.Value + "\"" : x.Value)));
        }

        public static bool TryParse(string input, out EnvironmentalVariableCollection result)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                result = new EnvironmentalVariableCollection();
                return true;
            }

            result = null;

            // http://stackoverflow.com/a/4780801/1086121
            var parts = Regex.Split(input.Trim(), "(?<=^[^\"]+(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            var finalResult = new EnvironmentalVariableCollection();
            foreach (var part in parts)
            {
                var subParts = part.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (subParts.Length != 2)
                    return false;

                if (subParts[0].Contains('"'))
                    return false;

                if (subParts[1].StartsWith("\"") != subParts[1].EndsWith("\""))
                    return false;

                if (finalResult.ContainsKey(subParts[0]))
                    return false;

                finalResult.Add(subParts[0], subParts[1].Trim('"'));
            }

            result = finalResult;
            return true;
        }
    }
}
