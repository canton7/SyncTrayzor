using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingPoller : IDisposable
    {
        void Start();
        void Stop();
    }

    public abstract class SyncThingPoller : ISyncThingPoller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly TimeSpan pollingInterval;
        private readonly TimeSpan erroredWaitInterval;

        private readonly object runningLock = new object();
        private CancellationTokenSource cancelCts;
        private bool _running;

        public void Start()
        {
            lock (this.runningLock)
            {
                if (this._running)
                    return;

                this.cancelCts = new CancellationTokenSource();
                this._running = true;
                this.StartInternal(this.cancelCts.Token);
            }
        }

        public void Stop()
        {
            CancellationTokenSource ctsToCancel = null;
            lock (this.runningLock)
            {
                if (!this._running)
                    return;

                this._running = false;
                ctsToCancel = this.cancelCts;
                this.cancelCts = null;
            }

            if (ctsToCancel != null)
                ctsToCancel.Cancel();
        }

        public SyncThingPoller(TimeSpan pollingInterval)
            : this(pollingInterval, TimeSpan.FromMilliseconds(1000))
        { }

        public SyncThingPoller(TimeSpan pollingInterval, TimeSpan erroredWaitInterval)
        {
            this.pollingInterval = pollingInterval;
            this.erroredWaitInterval = erroredWaitInterval;
        }

        protected virtual async void StartInternal(CancellationToken cancellationToken)
        {
            // We're aborted by the CTS
            while (!cancellationToken.IsCancellationRequested)
            {
                bool errored = false;

                try
                {
                    await this.PollAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    if (this.pollingInterval.Ticks > 0)
                        await Task.Delay(this.pollingInterval, cancellationToken);
                }
                catch (HttpRequestException)
                {
                    errored = true;
                }
                catch (IOException)
                {
                    // Socket forcibly closed. Could be a restart, could be a termination. We'll have to continue and quit if we're stopped
                    errored = true;
                }
                catch (OperationCanceledException e)
                {
                    // We can get cancels from tokens other than ours...
                    // If it was ours, then the while loop will abort shortly
                    if (e.CancellationToken != cancellationToken)
                        errored = true;
                }
                catch (Exception e)
                {
                    // Anything else?
                    // We can't abort, as then the exception will be lost. So log it, and keep going
                    logger.Error("Unexpected exception while polling", e);
                    errored = true;
                }

                if (errored)
                {
                    try
                    {
                        await Task.Delay(this.erroredWaitInterval, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    { }
                }
            }
        }

        protected abstract Task PollAsync(CancellationToken cancellationToken);

        public void Dispose()
        {
            this.Stop();
        }
    }
}
