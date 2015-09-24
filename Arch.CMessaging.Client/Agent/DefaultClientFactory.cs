using System;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Impl.Consumer;
using Arch.CMessaging.Client.Impl.Producer;
using Arch.CMessaging.Client.Impl.Producer.V09;
using System.Configuration;

namespace Arch.CMessaging.Client.Agent
{
    public class DefaultClientFactory : IProducerFactory, IConsumerFactory
    {
        static DefaultClientFactory clientFactory = new DefaultClientFactory();
        //static ProducerFactory producerFactory = new ProducerFactory();
        //ConsumerFactory consumerFactory = new ConsumerFactory();
        public static DefaultClientFactory GetInstance()
        {
            return clientFactory;
        }

        private DefaultClientFactory()
        {
        }
        [Obsolete]
        public IMessageProducer Create(string exchangeName, string identifier)
        {
            return ProducerFactory.Instance.Create(exchangeName, identifier);
        }
        [Obsolete]
        public ITopicConsumer CreateAsTopic(string topic, string exchangeName, string identifier)
        {
            //var userId = ConfigurationManager.AppSettings["UserId"];
            var consumer = ConsumerFactory.Instance.CreateAsTopic(topic, exchangeName, identifier);
            //consumer.TopicBind(topic, exchangeName, userId);
            return consumer;
        }
        [Obsolete]
        public IQueueConsumer CreateAsQueue(string exchangeName, string identifier)
        {
            return ConsumerFactory.Instance.CreateAsQueue(exchangeName, identifier);
        }
        [Obsolete]
        public IDeadLetterConsumer CreateDeadConsumer(string exchangeName, string identifier, string topic = null)
        {
            return ConsumerFactory.Instance.CreateAsDeadLetter(topic, exchangeName, identifier);
        }
    }
}
