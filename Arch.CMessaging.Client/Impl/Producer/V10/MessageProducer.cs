using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Client.Impl.Consumer;
using Arch.CMessaging.Core.Util;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class MessageProducer:IMessageProducer
    {
        public MessageProducer(ProducerChannel channel)
        {
            Guard.ArgumentNotNull(channel,"channel");

            Channel = channel;
            channel.BrokerAcks += (o, e) => { if (BrokerAcks != null && o.Identifier == Identifier) BrokerAcks(this, e); };
            channel.BrokerNacks += (o, e) => { if (BrokerNacks != null && o.Identifier == Identifier) BrokerNacks(this, e); };
            channel.CallbackException +=
                (e, r) =>
                    {
                        if (CallbackException == null) return;
                        var reader = r.Reader as ProducerMessageReader;
                        if (reader != null && reader.Identifier == Identifier)
                            CallbackException(e, r);
                    };
            channel.FlowControl += (o, e) => { if (FlowControl != null && o.Identifier == Identifier) FlowControl(this,e); };
        }

        private ProducerChannel Channel { get; set; }
        #region event
        public event Event.BrokerAckEventHandler BrokerAcks;

        public event Event.BrokerNackEventHandler BrokerNacks;

        public event Event.CallbackExceptionEventHandler CallbackException;

        public event Event.FlowControlEventHandler FlowControl;
        #endregion 

        public uint WindowSize { get; set; }

        public bool UseFlowControl { get; set; }

        public bool IsUnderFlowControl { get; private set; }

        public string Identifier { get; set; }

        private string ExchangeName { get; set; }

        public void ExchangeDeclare(string exchangeName)
        {
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");
            ExchangeName = exchangeName;
        }

        public void PublishAsync<TMessage>(TMessage message, string subject, Arch.CMessaging.Core.Content.MessageHeader header = null)
        {
            new Task(() =>
                         {
                             try
                             {
                                 var mw = new MessageWriter();
                                 var basicHeader = new BasicHeader {ExchangeName = ExchangeName, Subject = subject};
                                 if (header != null)
                                 {
                                     basicHeader.CorrelationID = header.CorrelationID;
                                     basicHeader.AppID = header.AppID;
                                     basicHeader.Sequence = header.Sequence;
                                     basicHeader.UserHeader = header.UserHeader;
                                 }
                                 mw.Write(message, basicHeader);
                                 var pubMessage = mw.ToMessage();
                                 Channel.PublishToBuffer(this, pubMessage);
                             }
                             catch (Exception ex)
                             {
                                 Logg.Write(ex,LogLevel.Error,"ProducerPublishAsyncError");
                                 if(CallbackException!=null)CallbackException(this,new CallbackExceptionEventArgs(ex,new ProducerMessageReader(null,Identifier)));
                             }
                         }).Start();
        }

        public void Dispose()
        {
        }
    }
}
