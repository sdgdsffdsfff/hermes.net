using System;
using Arch.CMessaging.Client.Consumer;
using Arch.CMessaging.Client.Consumer.Api;

namespace Arch.CMessaging.Client.Consumer.Engine
{
    public class Subscriber
    {

        private static MessageListenerConfig DEFAULT_MESSAGE_LISTENER_CONFIG = new MessageListenerConfig();

        public string GroupId { get; private set; }

        public string TopicPattern { get; private set; }

        public IMessageListener Consumer{ get; private set; }

        public ConsumerType ConsumerType{ get; private set; }

        public MessageListenerConfig MessageListenerConfig{ get; private set; }

        public Subscriber(String topicPattern, String groupId, IMessageListener consumer, MessageListenerConfig messageListenerConfig, ConsumerType consumerType)
        {
            TopicPattern = topicPattern;
            GroupId = groupId;
            Consumer = consumer;
            ConsumerType = consumerType;
            MessageListenerConfig = messageListenerConfig;
        }

        public Subscriber(String topicPattern, String groupId, IMessageListener consumer, ConsumerType consumerType)
            : this(topicPattern, groupId, consumer, DEFAULT_MESSAGE_LISTENER_CONFIG, consumerType)
        {
        }

        public Subscriber(String topicPattern, String groupId, IMessageListener consumer)
            : this(topicPattern, groupId, consumer, DEFAULT_MESSAGE_LISTENER_CONFIG, ConsumerType.DEFAULT)
        {
        }

        public Subscriber(String topicPattern, String groupId, IMessageListener consumer, MessageListenerConfig messageListenerConfig)
            : this(topicPattern, groupId, consumer, messageListenerConfig, ConsumerType.DEFAULT)
        {
        }

    }
}

