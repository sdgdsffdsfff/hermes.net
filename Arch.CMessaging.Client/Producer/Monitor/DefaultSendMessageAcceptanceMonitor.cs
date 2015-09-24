using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Arch.CMessaging.Client.Core.Future;
using Freeway.Logging;
using Arch.CMessaging.Client.Core.Ioc;
using System.Collections.Concurrent;

namespace Arch.CMessaging.Client.Producer.Monitor
{
    [Named(ServiceType = typeof(ISendMessageAcceptanceMonitor))]
    public class DefaultSendMessageAcceptanceMonitor : ISendMessageAcceptanceMonitor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultSendMessageAcceptanceMonitor));
        private ConcurrentDictionary<long, SettableFuture<bool>> futures = new ConcurrentDictionary<long, SettableFuture<bool>>();

        #region ISendMessageAcceptanceMonitor Members

        public IFuture<bool> Monitor(long correlationID)
        {
            var future = SettableFuture<bool>.Create();
            futures[correlationID] = future;
            return future;
        }

        public void Recieved(long correlationID, bool success)
        {
            Debug.WriteLine("Broker acceptance result is {0} for correlationId {1}", success, correlationID);
            SettableFuture<bool> future = null;
            futures.TryRemove(correlationID, out future);
            if (future != null)
            {
                future.Set(success);
            }
        }

        public void Cancel(long correlationId)
        {
            SettableFuture<bool> future = null;
            futures.TryRemove(correlationId, out future);
        }

        #endregion

    }
}
