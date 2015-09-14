using System;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Net.Core.Session;

namespace Arch.CMessaging.Client.Net.Core.Write
{
    class DefaultWriteRequestQueue : IWriteRequestQueue
    {
        private ConcurrentQueue<IWriteRequest> q = new ConcurrentQueue<IWriteRequest>();

        public Int32 Size
        {
            get { return q.Count; }
        }

        public IWriteRequest Poll(IoSession session)
        {
            IWriteRequest writeRequest = null;
            q.TryDequeue(out writeRequest);
            return writeRequest;
        }

        public void Offer(IoSession session, IWriteRequest writeRequest)
        {
            q.Enqueue(writeRequest);
        }

        public Boolean IsEmpty(IoSession session)
        {
            return q.IsEmpty;
        }

        public void Clear(IoSession session)
        {
            q = new ConcurrentQueue<IWriteRequest>();
        }

        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
