using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Core.Collections
{
    public class ThreadSafeQueue<T> : IEnumerable<T>, IEnumerable
    {
        private ConcurrentQueue<ReferenceWrapper<T>> queue;
        public ThreadSafeQueue() 
        {
            queue = new ConcurrentQueue<ReferenceWrapper<T>>();
        }
        public ThreadSafeQueue(IEnumerable<ReferenceWrapper<T>> collection)
        {
            queue = new ConcurrentQueue<ReferenceWrapper<T>>(collection);
        }
        public int Count { get { return queue.Count; } }
        public bool IsEmpty { get { return queue.IsEmpty; } }
        public void CopyTo(T[] array, int index)
        {
            if (array != null && array.Length > 0)
            {
                queue.CopyTo(array.Select(a => new ReferenceWrapper<T> { Item = a }).ToArray(), index);
            }
        }
        public void Enqueue(T item)
        {
            queue.Enqueue(new ReferenceWrapper<T> { Item = item });
        }
        public IEnumerator<T> GetEnumerator()
        {
            return queue.ToArray().Select(r => r.Item).GetEnumerator();
        }
        public T[] ToArray()
        {
            return queue.ToArray().Select(r => r.Item).ToArray();
        }
        public bool TryDequeue(out T result)
        {
            result = default(T);
            ReferenceWrapper<T> item;
            var hasElement = queue.TryDequeue(out item);
            if (hasElement)
            {
                result = item.Item;
                item.Item = default(T);
            }
            return hasElement;
        }

        public bool TryPeek(out T result)
        {
            result = default(T);
            ReferenceWrapper<T> item;
            var hasElement = queue.TryPeek(out item);
            if (hasElement)
            {
                result = item.Item;
            }
            return hasElement;
        }

        public struct ReferenceWrapper<TItem>
        {
            public TItem Item { get; set; }
        }

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
