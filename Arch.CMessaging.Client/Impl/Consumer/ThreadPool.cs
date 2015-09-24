using System;
using System.Threading;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class ThreadPool
    {
        private SemaphoreSlim _semaphore;
        private int _acquireTimeout;
        private int _maxPoolSize = Consts.Consumer_ConsumeAsyncThreadMax;
        private readonly object _lockObj = new object();

        public ThreadPool()
        {
            _acquireTimeout = Consts.Consumer_DefaultAcquireTimeout;
        }

        public int AcquireTimeout
        {
            get { return _acquireTimeout; }
            set
            {
                if (value > 0) _acquireTimeout = value;
            }
        }

        public int MaxPoolSize
        {
            get { return _maxPoolSize; }
            set
            {
                if (value <= Consts.Consumer_ConsumeAsyncThreadMax && value > 0)
                    _maxPoolSize = value;
            }
        }

        /// <summary>
        /// 此方法用于突破最大PoolSize（10）
        /// </summary>
        /// <param name="size"></param>
        public void ChangeMaxPoolSize(int size)
        {
            if (_maxPoolSize == size) return;
            _maxPoolSize = size;
            lock (_lockObj)
            {
                _semaphore = new SemaphoreSlim(MaxPoolSize);
            }
        }

        public int CurrentSize { get { return _semaphore.CurrentCount; } }

        public Thread Acquire()
        {
            if (_semaphore == null)
            {
                lock (_lockObj)
                {
                    if (_semaphore == null)
                    {
                        _semaphore = new SemaphoreSlim(MaxPoolSize);
                    }
                }
            }

            if (!_semaphore.Wait(AcquireTimeout))
            {
                throw new TimeoutException();
            }
            return new Thread(this);
        }

        public void Release()
        {
            if (_semaphore != null && CurrentSize<_maxPoolSize)
            {
                _semaphore.Release();
            }
        }

    }
}
