using System;
using System.Configuration;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Impl.Producer.V09;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class ProducerFactory:IProducerFactory
    {
        private IProducerChannel Channel { get; set; }

        private static ProducerFactory factory;
        private static object lockObject = new object();
        public static ProducerFactory Instance
        {
            get
            {
                if (factory == null)
                {
                    lock (lockObject)
                    {
                        if (factory == null)
                        {
                            factory = new ProducerFactory();
                            //Channel = new ChannelFactory().CreateChannel<ProducerChannel>(new MessageChannelConfigurator());
                        }
                    }
                }
                return factory;
            }
        }

        public ProducerFactory()
        {

        }

        private IProducerChannel CreateChannel()
        {
            if (Channel == null)
            {
                lock (lockObject)
                {
                    if (Channel == null)
                    {
                        Channel = DefaultMessageChannelFactory.Instance.CreateChannel<IProducerChannel>("");
                        Channel.Open(1000);
                    }
                }
            }
            return Channel;
        }

        public IMessageProducer Create(string exchangeName, string identifier)
        {
            Guard.ArgumentNotNullOrEmpty(identifier, "identifier");
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");

            //v0.9
            
            try
            {
                var channel = CreateChannel();
                var producer = channel.CreateProducer();

                //v1.0
                //var producer = Channel.CreateProducer();

                producer.Identifier = identifier;
                producer.ExchangeDeclare(exchangeName);
                return producer;
            }
            catch (Exception ex)
            {
                Logg.Write(ex,LogLevel.Error, "cmessaging.producer.producerfactory.create");
                var exceptionProducer = new ExceptionMessageProducer {Identifier = identifier};
                exceptionProducer.ExchangeDeclare(exchangeName);
                return exceptionProducer;
            }

        }
    }
}
