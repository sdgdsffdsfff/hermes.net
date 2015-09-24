using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace Arch.CMessaging.Client.Core.Collections
{
    /// <summary>
    /// 多生产者单消费者阻塞队列。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChunkedNotifyQueue<TItem> : IChunkedBlockingQueue<TItem>
    {
        private readonly ThreadSafeQueue<TItem> queue;
        private readonly ManualResetEventSlim mre;
        private readonly int capacity;
        private readonly int reachCountToNotify;
        private readonly int timeoutToNotify;
        private int currentCount = 0;
        public ChunkedNotifyQueue(int capacity, int reachCountToNotify, int timeoutToNotify)
        {
            this.capacity = capacity;
            this.reachCountToNotify = reachCountToNotify;
            this.timeoutToNotify = timeoutToNotify;
            this.queue = new ThreadSafeQueue<TItem>();
            this.mre = new ManualResetEventSlim(false);
        }

        public int Count { get { return currentCount; } }

        /// <summary>
        /// 将消息入队列，当消息数量达到指定值，通知消费者消费。
        /// </summary>
        /// <param name="item">消息</param>
        public bool Offer(TItem item)
        {
            int count = 0;
            var offerOk = false;
            if ((count = Interlocked.Increment(ref currentCount)) <= capacity)
            {
                queue.Enqueue(item);
                if (count >= reachCountToNotify)
                {
                    if (!mre.IsSet) mre.Set();
                }
                offerOk = true;
            }
            else
            {
                Interlocked.Decrement(ref currentCount);
            }
            return offerOk;
        }

        public TItem Take()
        {
            var item = default(TItem);
            var items = TakeBatch(1);
            if (items.Length > 0) item = items[0];
            return item;
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
                TItem item = default(TItem);
                var idx = 0;
                while (idx++ < maxElements && queue.TryDequeue(out item))
                {
                    items.Add(item);
                    Interlocked.Add(ref currentCount, -1);
                }
                return idx;
            }
        }

        /// <summary>
        /// 拉取消息，只能单线程。
        /// </summary>
        /// <param name="maxCount">一次最大取数</param>
        /// <returns></returns>
        public TItem[] TakeBatch(int maxCount)
        {
            while (true)
            {
                mre.Wait(timeoutToNotify);
                TItem[] items = null;
                int queueCount = queue.Count;
                if (queueCount > 0)
                {
                    items = new TItem[queueCount > maxCount ? maxCount : queueCount];
                    for (int i = 0; i < items.Length; i++)
                    {
                        TItem item = default(TItem);
                        if (queue.TryDequeue(out item))
                        {
                            items[i] = item;
                        }
                    }
                    Interlocked.Add(ref currentCount, -items.Length);
                    return items;
                }
                else
                {
                    Interlocked.Exchange(ref currentCount, 0);
                    if (mre.IsSet) mre.Reset();
                }
            }
        }

        /// <summary>
        /// for .net 4.0 bug
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        private struct ItemWrapper<T>
        {
            public T Value { get; set; }
        }
    }
}
