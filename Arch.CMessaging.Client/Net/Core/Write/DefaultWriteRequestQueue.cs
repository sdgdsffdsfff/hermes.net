using System;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Net.Core.Session;

namespace Arch.CMessaging.Client.Net.Core.Write
{
    class DefaultWriteRequestQueue : IWriteRequestQueue
    {
        private ConcurrentQueue<ItemWrapper<IWriteRequest>> q = new ConcurrentQueue<ItemWrapper<IWriteRequest>>();

        public Int32 Size
        {
            get { return q.Count; }
        }

        public IWriteRequest Poll(IoSession session)
        {
            IWriteRequest request = null;
            ItemWrapper<IWriteRequest> item;
            if (q.TryDequeue(out item))
            {
                request = item.Value;
                item.Value = null;
            }
            return request;
        }

        public void Offer(IoSession session, IWriteRequest writeRequest)
        {
            q.Enqueue(new ItemWrapper<IWriteRequest> { Value = writeRequest });
        }

        public Boolean IsEmpty(IoSession session)
        {
            return q.IsEmpty;
        }

        public void Clear(IoSession session)
        {
            q = new ConcurrentQueue<ItemWrapper<IWriteRequest>>();
        }

        public void Dispose(IoSession session)
        {
            // Do nothing
        }

        private struct ItemWrapper<T>
        {
            public T Value { get; set; }
        }
    }
}
