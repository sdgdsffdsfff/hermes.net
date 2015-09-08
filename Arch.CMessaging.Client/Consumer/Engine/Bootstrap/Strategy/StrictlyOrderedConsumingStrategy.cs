using System;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    [Named(ServiceType = typeof(IConsumingStrategy), ServiceName = "STRICTLY_ORDERING")]
    public class StrictlyOrderedConsumingStrategy : BaseCosumingStrategy
    {
        protected override IConsumerTask GetConsumerTask(ConsumerContext context, int partitionId, int localCacheSize)
        {
            return new StrictlyOrderedConsumingStrategyConsumerTask(context, partitionId, localCacheSize);
        }
    }
}

