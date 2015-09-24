using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Result;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Pipeline;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Producer.Build;
using Arch.CMessaging.Client.Core.Exceptions;

namespace Arch.CMessaging.Client.Producer
{
    [Named(ServiceType = typeof(Producer))]
    public class DefaultProducer : Producer
    {
        [Inject(BuildConstants.PRODUCER)]
        private IPipeline<IFuture<SendResult>> pipeline;
        
        [Inject]
        private ISystemClockService systemClockService;

        public override IMessageHolder Message(string topic, string partitionKey, object body)
        {
            if (String.IsNullOrWhiteSpace(topic))
            {
                throw new Exception("Topic can not be null or empty.");
            }
            return new DefaultMessageHolder(topic, partitionKey, body, pipeline, systemClockService);
        }

        private class DefaultMessageHolder : IMessageHolder
        {
            private ProducerMessage message;
            private IPipeline<IFuture<SendResult>> pipeline;
            private ISystemClockService systemClockService;

            public DefaultMessageHolder(
                string topic, 
                string partitionKey, 
                object body,
                IPipeline<IFuture<SendResult>> pipeline,
                ISystemClockService systemClockService)
            {
                this.message = new ProducerMessage(topic, body);
                this.message.PartitionKey = partitionKey;
                this.pipeline = pipeline;
                this.systemClockService = systemClockService;
            }

            #region IMessageHolder Members

            public IMessageHolder WithPriority()
            {
                message.IsPriority = true;
                return this;
            }

            public IMessageHolder WithRefKey(string key)
            {
                if (!string.IsNullOrEmpty(key) && key.Length > 90)
                    throw new ArgumentException(string.Format("RefKey's length must not larger than 90 characters(refKey={0})", key));
                message.Key = key;
                return this;
            }

            public IFuture<SendResult> Send()
            {

                message.BornTime = systemClockService.Now();
                return pipeline.Put(message);
            }

            public IMessageHolder AddProperty(string key, string value)
            {
                message.AddDurableAppProperty(key, value);
                return this;
            }

            public IMessageHolder SetCallback(ICompletionCallback<SendResult> callback)
            {
                message.Callback = callback;
                return this;
            }

            public SendResult SendSync()
            {
                try
                {
                    return Send().Get();
                }
                catch (Exception ex)
                {
                    throw new MessageSendException("send failed", ex);
                }
            }

            public IMessageHolder WithoutHeader()
            {
                message.WithHeader = false;
                return this;
            }

            #endregion
        }
    }
}
