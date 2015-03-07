using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class SemaphoreSlimExtensions
    {
        private class SemaphoreSlimLock : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            public SemaphoreSlimLock(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                this.semaphore.Release();
            }
        }

        public static IDisposable WaitDisposable(this SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new SemaphoreSlimLock(semaphore);
        }

        public static async Task<IDisposable> WaitAsyncDisposable(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new SemaphoreSlimLock(semaphore);
        }
    }
}
