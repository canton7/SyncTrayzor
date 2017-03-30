using System;
using System.Collections.Generic;
using System.Linq;

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

        // Stolen from http://stackoverflow.com/a/298990/1086121
        public static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;

            var split = commandLine.Split(c =>
            {
                if (c == '\"')
                    inQuotes = !inQuotes;

                return !inQuotes && c == ' ';
            });

            var result = split
                .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
                .Where(arg => !string.IsNullOrEmpty(arg));

            return result;
        }

        public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        public static string TrimMatchingQuotes(this string input, char quote)
        {
            if ((input.Length >= 2) &&
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        public static string JoinCommandLine(IEnumerable<string> input)
        {
            return String.Join(" ", input.Select(x => x.Contains(' ') ? $"\"{x.Replace("\"", "\\\"")}\"" : x));
        }
    }
}
