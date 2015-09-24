using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch.CMessaging.Client.Core.Collections
{
    public class ChunkedProducerConsumer<TItem> : AbstractProducerConsumer<TItem, ChunkedNotifyQueue<TItem>>
    {
        private const int ONE_ITEM_CHUNK = 1;
        private const int NOTIFY_AFTER_ONE_SECOND = 1;
        private const int ONLY_ONE_CONSUMER = 1;
        private int maxCountOneChunk;
        private ChunkedNotifyQueue<TItem> blockingQueue;
        public ChunkedProducerConsumer(int capacity)
            : this(capacity, ONLY_ONE_CONSUMER, NOTIFY_AFTER_ONE_SECOND, ONE_ITEM_CHUNK, ONE_ITEM_CHUNK) { }

        public ChunkedProducerConsumer(
            int capacity,
            int maxConsumerCount,
            int timeoutToNotify,
            int maxCountOneChunk,
            int reachCountToNotify)
            :base(maxConsumerCount)
        {
            this.maxCountOneChunk = maxCountOneChunk;
            this.blockingQueue = new ChunkedNotifyQueue<TItem>(capacity, reachCountToNotify, timeoutToNotify);
            base.StartPolling();
        }

        protected override ChunkedNotifyQueue<TItem> BlockingQueue
        {
            get { return blockingQueue; }
        }

        protected override IConsumingItem TakeConsumingItem()
        {
            return new ChunkedConsumingItem<TItem>(blockingQueue.TakeBatch(maxCountOneChunk));
        }
    }
}
