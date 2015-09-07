using System;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap
{
    [Named(ServiceType = typeof(IConsumerBootstrapManager))]
    public class DefaultConsumerBootstrapManager : IConsumerBootstrapManager
    {
        [Inject]
        private IConsumerBootstrapRegistry registry;

        public IConsumerBootstrap FindConsumerBootStrap(Topic topic)
        {

            if (Storage.KAFKA.Equals(topic.StorageType))
            {
                return registry.FindConsumerBootstrap(Endpoint.KAFKA);
            }
            else if (Endpoint.BROKER.Equals(topic.EndpointType) || Endpoint.KAFKA.Equals(topic.EndpointType))
            {
                return registry.FindConsumerBootstrap(topic.EndpointType);
            }
            else
            {
                throw new Exception(string.Format("Unknown endpoint type: {0}", topic.EndpointType));
            }

        }
    }
}

