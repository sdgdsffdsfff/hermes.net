using System;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using System.Collections.Concurrent;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    /// <summary>
    /// 内存管理
    /// </summary>
    internal sealed class MemoryManager
    {
        private ThreadSafe.Long _maxMemorySize;
        private ThreadSafe.Long _currentMemorySize;
        private readonly ConcurrentDictionary<string, int> _consumerMemory;
        public MemoryManager()
        {
            //_maxMemorySize = new ThreadSafe.Integer((int) Consts.Consumer_DefaultCapacity);
            _currentMemorySize = new ThreadSafe.Long(0);
            _consumerMemory=new ConcurrentDictionary<string, int>();
        }

        public long MaxMemorySize
        {
            get { return _maxMemorySize.ReadFullFence(); }
        }
        public long CurrentMemorySize
        {
            get { return _currentMemorySize.ReadFullFence(); }
        }

        public bool IsOutOfMaxMemorySize
        {
            get { return CurrentMemorySize >= MaxMemorySize; }
        }

        public void ChangeMaxMemorySize(long size)
        {
            _maxMemorySize.AtomicExchange(size);
        }

        public bool AtomicAdd(string consumerUri,int size)
        {
            if ((CurrentMemorySize + size) > MaxMemorySize) return false;
            _currentMemorySize.AtomicAddAndGet(size);
            int currentSize;
            if (_consumerMemory.TryGetValue(consumerUri, out currentSize))
            {
                if(_consumerMemory.TryUpdate(consumerUri,currentSize + size,currentSize))
                {
                    MetricUtil.Set(new MemoryMetric { Consumer = consumerUri }, currentSize + size);
                }
            }
            else
            {
                if (_consumerMemory.TryAdd(consumerUri, size))
                {
                    MetricUtil.Set(new MemoryMetric { Consumer = consumerUri }, size);
                }
            }
            return true;
        }

        public void AtomicReduce(string consumerUri, int size)
        {
            var tmp = 0 - Math.Abs(size);
            _currentMemorySize.AtomicAddAndGet(tmp);

            int currentSize;
            if (_consumerMemory.TryGetValue(consumerUri, out currentSize))
            {
                if (_consumerMemory.TryUpdate(consumerUri, currentSize - size, currentSize))
                {
                    MetricUtil.Set(new MemoryMetric { Consumer = consumerUri }, currentSize - size);
                }
            }
            else
            {
                if (_consumerMemory.TryAdd(consumerUri, -size))
                {
                    MetricUtil.Set(new MemoryMetric { Consumer = consumerUri }, -size);
                }
            }
        }
    }
}
