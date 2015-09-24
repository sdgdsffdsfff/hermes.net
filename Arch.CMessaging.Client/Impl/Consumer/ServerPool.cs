using System;
using System.Threading;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class ServerPool 
    {
        private SemaphoreSlim _semaphore;
        private int _acquireTimeout;
        private int _maxPoolSize;
        private readonly object _lockObj = new object();
        private readonly IService _server;
        public ServerPool(IService server)
        {
            Guard.ArgumentNotNull(server, "server");

            _server = server;
            _acquireTimeout = Consts.Consumer_DefaultAcquireTimeout;
            _maxPoolSize = ConfigUtil.Instance.ConnectionMax;
        }
        public int AcquireTimeout
        {
            get { return _acquireTimeout; }
            set
            {
                if (value > Consts.Consumer_DefaultAcquireTimeout)
                    _acquireTimeout = value;
            }
        }

        public int MaxPoolSize
        {
            get { return _maxPoolSize; }
            set
            {
                if (value < ConfigUtil.Instance.ConnectionMax && value > 0)
                    _maxPoolSize = value;
            }
        }

        public IService Acquire()
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
            return _server;
        }
        /// <summary>
        /// TSERVER操作完成时，必须调用
        /// </summary>
        public void Release()
        {
            if (_semaphore != null)
                _semaphore.Release();
        }
    }
}
