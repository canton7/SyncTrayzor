using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    // Taken from https://msdn.microsoft.com/en-us/library/ee789351%28v=vs.110%29.aspx
    public class LimitedConcurrencyTaskScheduler : TaskScheduler
    {
        // Indicates whether the current thread is processing work items.
        private readonly ThreadLocal<bool> currentThreadIsProcessingItems = new ThreadLocal<bool>();

        // The list of tasks to be executed  
        private readonly LinkedList<Task> tasks = new LinkedList<Task>(); // protected by lock(_tasks) 

        // The maximum concurrency level allowed by this scheduler.  
        private readonly int maxDegreeOfParallelism;

        // Indicates whether the scheduler is currently processing work items.  
        private int delegatesQueuedOrRunning = 0;

        // Creates a new instance with the specified degree of parallelism.  
        public LimitedConcurrencyTaskScheduler(int maxDegreeOfParallelism)
        {
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
            this.maxDegreeOfParallelism = maxDegreeOfParallelism;
        }

        // Queues a task to the scheduler.  
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough  
            // delegates currently queued or running to process tasks, schedule another.  
            lock (this.tasks)
            {
                this.tasks.AddLast(task);
                if (this.delegatesQueuedOrRunning < this.maxDegreeOfParallelism)
                {
                    ++this.delegatesQueuedOrRunning;
                    this.NotifyThreadPoolOfPendingWork();
                }
            }
        }

        // Inform the ThreadPool that there's work to be executed for this scheduler.  
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items. 
                // This is necessary to enable inlining of tasks into this thread.
                this.currentThreadIsProcessingItems.Value = true;
                try
                {
                    // Process all available items in the queue. 
                    while (true)
                    {
                        Task item;
                        lock (this.tasks)
                        {
                            // When there are no more items to be processed, 
                            // note that we're done processing, and get out. 
                            if (this.tasks.Count == 0)
                            {
                                --this.delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = this.tasks.First.Value;
                            this.tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue 
                        base.TryExecuteTask(item);
                    }
                }
                // We're done processing items on the current thread 
                finally
                {
                    this.currentThreadIsProcessingItems.Value = false;
                }
            }, null);
        }

        // Attempts to execute the specified task on the current thread.  
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining 
            if (!this.currentThreadIsProcessingItems.Value)
                return false;

            // If the task was previously queued, remove it from the queue 
            if (taskWasPreviouslyQueued)
            {
                // Try to run the task.  
                if (this.TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            }
            else
            {
                return base.TryExecuteTask(task);
            }
        }

        // Attempt to remove a previously scheduled task from the scheduler.  
        protected sealed override bool TryDequeue(Task task)
        {
            lock (this.tasks)
            {
                return this.tasks.Remove(task);
            }
        }

        // Gets the maximum concurrency level supported by this scheduler.  
        public sealed override int MaximumConcurrencyLevel => this.maxDegreeOfParallelism;

        // Gets an enumerable of the tasks currently scheduled on this scheduler.  
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(this.tasks, ref lockTaken);
                if (lockTaken)
                    return this.tasks;
                else
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(this.tasks);
            }
        }
    }
}
