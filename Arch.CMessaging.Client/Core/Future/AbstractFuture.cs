using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Core.Future
{
    public abstract class AbstractFuture<T> : IListenableFuture<T>, IDisposable
    {
        private object val;
        private bool disposed;
        private volatile Boolean ready;
        private ThreadSafe.AtomicReference<ProducerConsumer<FutureCallbackItem<T>>> executor = new ThreadSafe.AtomicReference<ProducerConsumer<FutureCallbackItem<T>>>(null);
        private IList<IFutureCallback<T>> callbackList = new List<IFutureCallback<T>>();
        private readonly ManualResetEventSlim readyEvent = new ManualResetEventSlim(false);
        private object syncRoot = new object();
        
        #region IFuture<T> Members

        public bool IsCancelled { get; private set; }

        public bool IsDone
        {
            get { return ready; }
        }

        public T Get()
        {
            return Get(Timeout.Infinite);
        }

        public T Get(int timeoutInMills)
        {
            T result = default(T);
            if (!Await(timeoutInMills)) throw new TimeoutException();
            else
            {
                if (Value is Exception) throw Value as Exception;
                else result = (T)Value;
            }
            return result;
        }

        public virtual bool Cancel(bool mayInterruptIfRunning)
        {
            IsCancelled = true;
            Value = new OperationCanceledException();
            return IsCancelled;
        }

        #endregion

        #region IListenableFuture<T> Members
        public void AddListener(IFutureCallback<T> callback, ProducerConsumer<FutureCallbackItem<T>> executor)
        {
            this.executor.AtomicCompareExchange(executor, null);
            lock (syncRoot)
            {
                callbackList.Add(callback);
            }
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ((IDisposable)readyEvent).Dispose();
                    disposed = true;
                }
            }
        }

        protected object Value 
        {
            get { return val; }
            set
            {
                lock (this)
                {
                    if (ready) return;
                    ready = true;
                    val = value;
                    readyEvent.Set();
                    FireCallback(val);
                }
            }
        }

        private bool Await(int timeoutInMills)
        {
            if (ready) return ready;

            readyEvent.Wait(timeoutInMills);
            if (ready) readyEvent.Dispose();

            return ready;
        }

        private void FireCallback(object val)
        {
            IFutureCallback<T>[] callbacks;
            lock (syncRoot)
            {
                callbacks = callbackList.ToArray();
            }
            if (executor != null)
            {
                var producer = this.executor.ReadFullFence();
                if (producer != null)
                {
                    foreach (var callback in callbacks)
                        producer.Produce(new FutureCallbackItem<T> { Item = val, Callback = callback });
                }
            }
        }
    }
}
