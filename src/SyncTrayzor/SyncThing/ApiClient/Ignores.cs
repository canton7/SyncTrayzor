using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SyncTrayzor.SyncThing.ApiClient
{
    public class Ignores
    {
        private List<string> _ignorePatterns = new List<string>();

        [JsonProperty("ignore")]
        public List<string> IgnorePatterns
        {
            get { return this._ignorePatterns; }
            set { this._ignorePatterns = (value ?? new List<string>()); }
        }

        private List<string> _regexPatterns;

        [JsonProperty("patterns")]
        public List<string> RegexPatterns
        {
            get { return this._regexPatterns; }
            set { this._regexPatterns = (value ?? new List<string>()); }
        }

        public override string ToString()
        {
            return $"<Ignores ignore=[{String.Join(", ", this.IgnorePatterns)}] patterns=[{String.Join(", ", this.RegexPatterns)}]>";
        }
    }
}
