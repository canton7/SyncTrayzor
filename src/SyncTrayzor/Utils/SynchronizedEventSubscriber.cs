using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class SynchronizedEventSubscriber
    {
        private readonly SynchronizationContext synchronizationContext;

        public SynchronizedEventSubscriber(bool allowNoSynchronization = false)
            : this(SynchronizationContext.Current, allowNoSynchronization)
        { }

        public SynchronizedEventSubscriber(SynchronizationContext synchronizationContext, bool allowNoSynchronization = false)
        {
            if (!allowNoSynchronization && synchronizationContext == null)
                throw new ArgumentException("synchronizationContext must be non-null if allowNoSynchronizationContext is false");

            this.synchronizationContext = synchronizationContext;
        }

        public EventHandler Subscribe(EventHandler forwarder)
        {
            return (o, e) => this.Post(_ => forwarder(o, e), null);
        }

        public EventHandler<T> Subscribe<T>(EventHandler<T> forwarder)
        {
            return (o, e) => this.Post(_ => forwarder(o, e), null);
        }

        private void Post(SendOrPostCallback callback, object state)
        {
            if (this.synchronizationContext != null)
                this.synchronizationContext.Post(callback, state);
            else
                callback(state);
        }
    }
}
