using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Arch.CMessaging.Client.Core.Collections
{
    public class BlockingQueue<TItem> : IBlockingQueue<TItem>
    {
        private Queue<TItem> queue;
        private int capacity;
        private int waitOnPutting;
        private int waitOnTaking;
        private object syncRoot = new object();

        public BlockingQueue(int capacity)
        {
            this.capacity = capacity;
            this.queue = new Queue<TItem>();
        }

        public int RemainingCapacity
        {
            get
            {
                lock (syncRoot) { return capacity - queue.Count; }
            }
        }

        public int Count
        {
            get
            {
                lock (syncRoot) { return queue.Count; }
            }
        }

        public bool Put(TItem item, int timeoutInMills)
        {
            try
            {
                Monitor.Enter(syncRoot);
                if (queue.Count >= capacity)
                {
                    waitOnPutting++;
                    if (!Monitor.Wait(syncRoot, timeoutInMills)) return false;
                }
                queue.Enqueue(item);
                if (waitOnTaking < 1) return true;
                waitOnTaking--;
                Monitor.Pulse(syncRoot);
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
            return true;
        }

        public virtual bool Offer(TItem item)
        {
            try
            {
                Monitor.Enter(syncRoot);
                if (queue.Count >= capacity) return false;
                queue.Enqueue(item);
                if (waitOnTaking < 1) return true;
                waitOnTaking--;
                Monitor.Pulse(syncRoot);
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
            return true;
        }


        public TItem Take()
        {
            TItem val;
            try
            {
                Monitor.Enter(syncRoot);
                if (queue.Count > 0)
                {
                    val = queue.Dequeue();
                    if (waitOnPutting > 0)
                    {
                        waitOnPutting--;
                        Monitor.Pulse(syncRoot);
                    }
                }
                else
                {
                    waitOnTaking++;
                    Monitor.Wait(syncRoot);
                    val = this.Take();
                }
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }

            return val;
        }

        public int DrainTo(IList<TItem> items)
        {
            return DrainTo(items, capacity);
        }

        public int DrainTo(IList<TItem> items, int maxElements)
        {
            if (items == null) return 0;
            else
            {
                var idx = 0;
                try
                {
                    Monitor.Enter(syncRoot);
                    while (idx++ < maxElements && queue.Count > 0)
                    {
                        items.Add(queue.Dequeue());
                    }
                }
                finally
                {
                    Monitor.Exit(syncRoot);
                }
                return idx;
            }
        }

        public TItem Peek()
        {
            lock (syncRoot)
            {
                return queue.Count == 0 ? default(TItem) : queue.Peek();
            }
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                queue.Clear();
            }
        }
    }
}
