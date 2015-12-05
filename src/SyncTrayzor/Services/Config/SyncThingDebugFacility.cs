using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SyncTrayzor.Services.Config
{
    public class SyncThingDebugFacility
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; }

        public SyncThingDebugFacility()
        {
        }

        public SyncThingDebugFacility(string name, string description, bool isEnabled)
        {
            this.Name = name;
            this.Description = description;
            this.IsEnabled = isEnabled;
        }

        public override string ToString()
        {
            return $"<DebugFacility Name={this.Name} IsEnabled={this.IsEnabled}>";
        }
    }
}
