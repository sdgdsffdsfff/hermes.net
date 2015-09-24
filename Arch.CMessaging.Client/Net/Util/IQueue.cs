using System;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Collections;

namespace Arch.CMessaging.Client.Net.Util
{
    public interface IQueue<T> : IEnumerable<T>
    {
        Boolean IsEmpty { get; }
        void Enqueue(T item);
        T Dequeue();
        Int32 Count { get; }
    }

    class Queue<T> : System.Collections.Generic.Queue<T>, IQueue<T>
    {
        public Boolean IsEmpty
        {
            get { return base.Count == 0; }
        }

        T IQueue<T>.Dequeue()
        {
            return IsEmpty ? default(T) : base.Dequeue();
        }
    }

    class ConcurrentQueue<T> : ThreadSafeQueue<T>, IQueue<T>  // System.Collections.Concurrent.ConcurrentQueue<T>, IQueue<T>
    {
        public T Dequeue()
        {
            T e = default(T);
            this.TryDequeue(out e);
            return e;
        }
    }
}
