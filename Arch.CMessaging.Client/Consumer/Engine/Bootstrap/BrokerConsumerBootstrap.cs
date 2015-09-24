using System;
using System.Collections.Generic;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap
{
    [Named(ServiceType = typeof(IConsumerBootstrap), ServiceName = Endpoint.BROKER)]
    public class BrokerConsumerBootstrap : BaseConsumerBootstrap
    {
        [Inject]
        private IConsumingStrategyRegistry ConsumingStrategyRegistry;

        protected override ISubscribeHandle DoStart(ConsumerContext context)
        {

            CompositeSubscribeHandle handler = new CompositeSubscribeHandle();

            List<Partition> partitions = MetaService.ListPartitionsByTopic(context.Topic.Name);
            IConsumingStrategy consumingStrategy = ConsumingStrategyRegistry.FindStrategy(context.ConsumerType);
            foreach (Partition partition in partitions)
            {
                handler.AddSubscribeHandle(consumingStrategy.Start(context, partition.ID));
            }

            return handler;
        }
    }
}

