using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.Api
{
    public interface IEventVisitor
    {
        void Accept(GenericEvent evt);
        void Accept(RemoteIndexUpdatedEvent evt);
        void Accept(LocalIndexUpdatedEvent evt);
        void Accept(StateChangedEvent evt);
        void Accept(ItemStartedEvent evt);
    }
}
