namespace SyncTrayzor.SyncThing.DebugFacilities
{
    public class DebugFacility
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsEnabled { get; set; }

        public DebugFacility(string name, string description, bool isEnabled)
        {
            this.Name = name;
            this.Description = description;
            this.IsEnabled = isEnabled;
        }
    }
}
