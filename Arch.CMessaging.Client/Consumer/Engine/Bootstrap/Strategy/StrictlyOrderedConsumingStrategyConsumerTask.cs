using System;
using Freeway.Logging;
using Arch.CMessaging.Core.Util;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Core.Schedule;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public class StrictlyOrderedConsumingStrategyConsumerTask : BaseConsumerTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StrictlyOrderedConsumingStrategyConsumerTask));


        public StrictlyOrderedConsumingStrategyConsumerTask(ConsumerContext context, int partitionId, int cacheSize)
            : base(context, partitionId, cacheSize)
        {
        }

        protected override void DoBeforeConsuming(ConsumerLeaseKey key, long correlationId)
        {
        }

        protected override void DoAfterConsuming(ConsumerLeaseKey key, long correlationId)
        {
        }

        protected override BasePullMessagesTask GetPullMessageTask()
        {
            return null;
        }

    }
}

