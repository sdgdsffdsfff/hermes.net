using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Freeway.Logging;

namespace Arch.CMessaging.Client.Core.Collections
{
    public abstract class AbstractProducerConsumer<TItem, TQueue> : IProducerConsumer<TItem, TQueue>
        where TQueue : IBlockingQueue<TItem>
    {
        private Thread pollingThread;
        private SemaphoreSlim semaphore;
        private const int ONLY_ONE_CONSUMER = 1;
        private static readonly ILog log = LogManager.GetLogger(typeof(AbstractProducerConsumer<TItem, TQueue>));
        public AbstractProducerConsumer()
            : this(ONLY_ONE_CONSUMER) { }
        public AbstractProducerConsumer(int maxConsumerCount)
        {
            this.semaphore = new SemaphoreSlim(maxConsumerCount, maxConsumerCount);
            pollingThread = new Thread(InfinitePolling);
            pollingThread.IsBackground = true;
        }

        public bool Produce(TItem item)
        {
            return BlockingQueue.Offer(item);
        }

        private void InfinitePolling()
        {
            while (true)
            {
                semaphore.Wait();
                try
                {
                    var item = TakeConsumingItem();
                    if (item != null)
                    {
                        ThreadPool.QueueUserWorkItem((state) =>
                            {
                                var consumingItem = state as IConsumingItem;
                                try
                                {
                                    if (OnConsume != null)
                                        OnConsume(this, new ConsumeEventArgs(consumingItem));
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex);
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            }, item);
                    }
                    else semaphore.Release();
                }
                catch (ThreadAbortException)
                {
                    semaphore.Dispose();
                }
                catch (Exception)
                {
                    semaphore.Release();
                }
            }
        }

        public event EventHandler<ConsumeEventArgs> OnConsume;
        public void Shutdown()
        {
            pollingThread.Abort();   
        }

        protected void StartPolling()
        {
            pollingThread.Start();
        }
        protected abstract TQueue BlockingQueue { get; }
        protected abstract IConsumingItem TakeConsumingItem();
    }
}
