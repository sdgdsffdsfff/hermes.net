using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class ExceptionMessageProducer : IMessageProducer
    {
        public event Event.BrokerAckEventHandler BrokerAcks;

        public event Event.BrokerNackEventHandler BrokerNacks;

        public event Event.CallbackExceptionEventHandler CallbackException;

        public event Event.FlowControlEventHandler FlowControl;

        public uint WindowSize
        { get; set; }

        public bool UseFlowControl
        { get; set; }

        public bool IsUnderFlowControl
        {
            get { return UseFlowControl; }
        }

        public string Identifier
        { get; set; }

        private string _exchangeName;
        public void ExchangeDeclare(string exchangeName)
        {
            if (string.IsNullOrEmpty(exchangeName))
            {
                return;
            }
            if (string.IsNullOrEmpty(_exchangeName))
            {
                _exchangeName = exchangeName.Trim();
            }
        }

        private IMessageProducer messageProducer;

        public void PublishAsync<TMessage>(TMessage message, string subject, Arch.CMessaging.Core.Content.MessageHeader header = null)
        {
            if(messageProducer != null)
            {
                messageProducer.PublishAsync<TMessage>(message,subject,header);
            }
            else
            {
                var channel = DefaultMessageChannelFactory.Instance.CreateChannel<IProducerChannel>("");
                channel.Open(1000);
                messageProducer = channel.CreateProducer();

                messageProducer.Identifier = Identifier;
                messageProducer.ExchangeDeclare(_exchangeName);
                messageProducer.PublishAsync<TMessage>(message, subject, header);
            }
        }

        public void Dispose()
        {
        }
    }
}
