using System;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Consumer.Engine.Notifier;
using Arch.CMessaging.Client.Transport.EndPoint;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Consumer.Engine.Config;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Core.Env;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.MetaService;
using Arch.CMessaging.Client.Core.Message.Retry;
using System.Threading;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    [Named(ServiceType = typeof(IConsumingStrategy), ServiceName = "DEFAULT")]
    public class DefaultConsumingStrategy : BaseConsumingStrategy
    {
        protected override IConsumerTask GetConsumerTask(ConsumerContext context, int partitionId, int localCacheSize)
        {
            return new DefaultConsumingStrategyConsumerTask(context, partitionId, localCacheSize);
        }

    }
}

