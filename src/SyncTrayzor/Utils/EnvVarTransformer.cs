using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class EnvVarTransformer
    {
        private static readonly Regex varRegex = new Regex(@"%(\w+)%");
        private static readonly Dictionary<string, string> specials;

        static EnvVarTransformer()
        {
            specials = new Dictionary<string, string>()
            {
                { "EXEPATH", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) }
            };
        }

        public static string Transform(string input)
        {
            return varRegex.Replace(input, match =>
            {
                var name = match.Groups[1].Value;
                if (specials.ContainsKey(name))
                    return specials[name];
                return Environment.GetEnvironmentVariable(name);
            });
        }
    }
}
