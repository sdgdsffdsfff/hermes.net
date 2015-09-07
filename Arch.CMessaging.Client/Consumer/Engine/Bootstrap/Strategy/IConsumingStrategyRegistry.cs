using System;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
	public interface IConsumingStrategyRegistry
	{
		IConsumingStrategy FindStrategy (ConsumerType consumerType);
	}
}

