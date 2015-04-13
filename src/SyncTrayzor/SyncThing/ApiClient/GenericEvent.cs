using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return String.Format("<GenericEvent ID={0} Type={1} Time={2}>", this.Id, this.Type, this.Time);
        }
    }
}
