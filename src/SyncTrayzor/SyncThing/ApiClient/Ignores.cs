using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return String.Format("<Ignores ignore=[{0}] patterns=[{1}]>", String.Join(", ", this.IgnorePatterns), String.Join(", ", this.RegexPatterns));
        }
    }
}
