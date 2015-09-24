using System.Collections.Concurrent;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Util;
using System;
using System.IO;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    /// <summary>
    /// 用于快速生成Consumer的工厂
    /// <remarks>
    /// 这是一个简化的过程，如果需要处理异常，内存控制，限流等操作
    /// 请参考使用<see cref="IMessageChannelFactory"/>
    /// </remarks>
    /// </summary>
    public class ConsumerFactory : IConsumerFactory
    {
        ConcurrentDictionary<string, AbstractConsumer> consumerCache = new ConcurrentDictionary<string, AbstractConsumer>();
        private static ConsumerFactory factory;
        private static object lockObject = new object();
        public static ConsumerFactory Instance
        {
            get
            {
                if (factory == null)
                {
                    lock (lockObject)
                    {
                        if (factory == null)
                        {
                            factory = new ConsumerFactory();
                        }
                    }
                }
                return factory;
            }
        }

        internal ConsumerChannel Channel { get; private set; }
        public ConsumerFactory()
        {
            ObjectFactoryLifetimeManager.Instance.Register();
            Channel = new ChannelFactory().CreateChannel<ConsumerChannel>(new DefaultMessageChannelConfigurator());
        }

        /// <summary>
        /// 快速生成一个基于Topic语义消费的Consumer
        /// </summary>
        /// <param name="topic">订阅主题</param>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">标识Consumer的身份</param>
        /// <returns><see cref="ITopicConsumer"/></returns>
        public ITopicConsumer CreateAsTopic(string topic, string exchangeName, string identifier)
        {
            Guard.ArgumentNotNullOrEmpty(topic, "topic");
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            Guard.ArgumentNotNullOrEmpty(identifier, "identifier");
            topic = topic.Trim();
            exchangeName = exchangeName.Trim();
            identifier = identifier.Trim();

            var key = string.Format("{0}_{1}_{2}_TOPIC", exchangeName, topic, identifier);
            AbstractConsumer messageConsumer;
            if (consumerCache.TryGetValue(key, out messageConsumer))
            {
                if (!messageConsumer.IsDispose)
                {
                    var topicConsumer = messageConsumer as ITopicConsumer;
                    if (topicConsumer != null) return topicConsumer;
                }
                consumerCache.TryRemove(key, out messageConsumer);
            }
            var consumer = Channel.CreateConsumer<TopicConsumer>();
            consumer.Identifier = identifier;
            consumer.TopicBind(topic, exchangeName);
            consumerCache.TryAdd(key, consumer);

            return consumer;
        }

        /// <summary>
        /// 快速生成一个基于Queue语义消费的Consumer
        /// </summary>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">标识Consumer的身份</param>
        /// <returns><see cref="IQueueConsumer"/></returns>
        public IQueueConsumer CreateAsQueue(string exchangeName, string identifier)
        {
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            Guard.ArgumentNotNullOrEmpty(identifier, "identifier");

            exchangeName = exchangeName.Trim();
            identifier = identifier.Trim();

            var key = string.Format("{0}_{1}_QUEUE", exchangeName, identifier);
            AbstractConsumer messageConsumer;
            if (consumerCache.TryGetValue(key, out messageConsumer))
            {
                if (!messageConsumer.IsDispose)
                {
                    var queueConsumer = messageConsumer as IQueueConsumer;
                    if (queueConsumer != null) return queueConsumer;
                }
                consumerCache.TryRemove(key, out messageConsumer);
            }

            var consumer = Channel.CreateConsumer<QueueConsumer>();
            consumer.Identifier = identifier;
            consumer.QueueBind(exchangeName);
            consumerCache.TryAdd(key, consumer);
            return consumer;
        }

        /// <summary>
        /// 快速生成一个基于DeadLetter语义消费的Consumer
        /// </summary>
        /// <param name="topic">订阅主题</param>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">标识Consumer的身份</param>
        /// <returns><see cref="IQueueConsumer"/></returns>
        public IDeadLetterConsumer CreateAsDeadLetter(string topic, string exchangeName, string identifier)
        {
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            Guard.ArgumentNotNullOrEmpty(identifier, "identifier");
            exchangeName = exchangeName.Trim();
            identifier = identifier.Trim();

            var key = string.Format("{0}_{1}_{2}_DEAD", exchangeName, identifier,string.IsNullOrEmpty(topic)?"":topic);
            AbstractConsumer messageConsumer;
            if (consumerCache.TryGetValue(key, out messageConsumer))
            {
                if (!messageConsumer.IsDispose)
                {
                    var deadletterConsumer = messageConsumer as IDeadLetterConsumer;
                    if (deadletterConsumer != null) return deadletterConsumer;
                }
                consumerCache.TryRemove(key, out messageConsumer);
            }
            var consumer = Channel.CreateConsumer<DeadLetterConsumer>();
            consumer.Identifier = identifier;
            consumer.DeadLetterBind(exchangeName, topic);
            consumerCache.TryAdd(key, consumer);
            return consumer;
        }
    }
}
