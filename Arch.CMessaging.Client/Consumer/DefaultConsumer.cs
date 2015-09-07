using System;
using Arch.CMessaging.Client.Consumer.Engine;
using Arch.CMessaging.Client.Core.Ioc;
using System.Collections.Generic;
using Arch.CMessaging.Client.Consumer.Api;

namespace Arch.CMessaging.Client.Consumer
{
    [Named(ServiceType = typeof(Consumer))]
    public class DefaultConsumer : Consumer
    {

        private static MessageListenerConfig DEFAULT_MESSAGE_LISTENER_CONFIG = new MessageListenerConfig();

        [Inject]
        private IEngine engine;

        private IConsumerHolder Start(string topicPattern, string groupId, IMessageListener listener, MessageListenerConfig listenerConfig, ConsumerType consumerType)
        {
            
            ISubscribeHandle subscribeHandle = engine.Start(new Subscriber(topicPattern, groupId, listener, listenerConfig, consumerType));

            return new DefaultConsumerHolder(subscribeHandle);
        }

        public  override IConsumerHolder Start(string topic, string groupId, IMessageListener listener)
        {
            return Start(topic, groupId, listener, DEFAULT_MESSAGE_LISTENER_CONFIG);
        }

        public override IConsumerHolder Start(string topic, string groupId, IMessageListener listener, MessageListenerConfig config)
        {
            return Start(topic, groupId, listener, config, config.StrictlyOrdering ? ConsumerType.STRICTLY_ORDERING : ConsumerType.DEFAULT);
        }

        public override List<IMessageStream> CreateMessageStreams(string topic, string groupId)
        {
            // TODO Auto-generated method stub
            throw new NotImplementedException();
        }

        public class DefaultConsumerHolder : IConsumerHolder
        {

            private ISubscribeHandle subscribeHandle;

            public DefaultConsumerHolder(ISubscribeHandle subscribeHandle)
            {
                this.subscribeHandle = subscribeHandle;
            }

            public bool IsConsuming()
            {
                if (subscribeHandle is CompositeSubscribeHandle)
                {
                    return ((CompositeSubscribeHandle)subscribeHandle).ChildHandles.Count > 0;
                }
                return false;
            }

            public void Close()
            {
                subscribeHandle.Close();
            }

        }
    }
}

