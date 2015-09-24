using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Core.Collections
{
    public class ProducerConsumer<TItem> : AbstractProducerConsumer<TItem, BlockingQueue<TItem>>
    {
        private BlockingQueue<TItem> blockingQueue;
        public ProducerConsumer(int capacity) : this(capacity, 1) { }
        public ProducerConsumer(int capacity, int maxConsumerCount)
            : base(maxConsumerCount)
        {
            this.blockingQueue = new BlockingQueue<TItem>(capacity);
            base.StartPolling();
        }

        public ProducerConsumer(BlockingQueue<TItem> blockingQueue, int maxConsumerCount)
            : base(maxConsumerCount)
        {
            this.blockingQueue = blockingQueue;
            base.StartPolling();
        }

        protected override BlockingQueue<TItem> BlockingQueue
        {
            get { return blockingQueue; }
        }

        protected override IConsumingItem TakeConsumingItem()
        {
            return new SingleConsumingItem<TItem>(blockingQueue.Take());
        }
    }
}
