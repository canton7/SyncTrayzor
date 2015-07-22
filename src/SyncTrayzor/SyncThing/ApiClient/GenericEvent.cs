namespace SyncTrayzor.SyncThing.ApiClient
{
    public class GenericEvent : Event
    {
        public override void Visit(IEventVisitor visitor)
        {
            visitor.Accept(this);
        }

        public override string ToString()
        {
            return $"<GenericEvent ID={this.Id} Type={this.Type} Time={this.Time}>";
        }
    }
}
