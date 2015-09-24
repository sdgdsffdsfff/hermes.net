using System;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Producer.Build;
using System.Collections.Generic;
using Arch.CMessaging.Client.Consumer.Api;

namespace Arch.CMessaging.Client.Consumer
{
    public abstract class Consumer
    {
        public static Consumer GetInstance()
        {
            ComponentsConfigurator.DefineComponents();
            return ComponentLocator.Lookup<Consumer>();
        }

        public abstract IConsumerHolder Start(String topic, String groupId, IMessageListener listener);

        public abstract IConsumerHolder Start(String topicPattern, String groupId, IMessageListener listener, MessageListenerConfig config);

        public abstract List<IMessageStream> CreateMessageStreams(String topic, String groupId);
    }

    public interface IConsumerHolder
    {
        void Close();
    }
}

