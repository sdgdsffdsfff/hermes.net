using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    [Named(ServiceType = typeof(IConsumingStrategyRegistry))]
    public class DefaultConsumingRegistry : IConsumingStrategyRegistry, IInitializable
    {
        private ConcurrentDictionary<ConsumerType, IConsumingStrategy> Strategies = new ConcurrentDictionary<ConsumerType, IConsumingStrategy>();

        public void Initialize()
        {
            IDictionary<string, IConsumingStrategy> strategies = ComponentLocator.LookupMap<IConsumingStrategy>();

            foreach (KeyValuePair<string, IConsumingStrategy> entry in strategies)
            {
                ConsumerType consumerType;
                Enum.TryParse<ConsumerType>(entry.Key, out consumerType);
                Strategies[consumerType] = entry.Value;
            }
        }

        public IConsumingStrategy FindStrategy(ConsumerType consumerType)
        {
            return CollectionUtil.TryGet(Strategies, consumerType);
        }
    }
}

