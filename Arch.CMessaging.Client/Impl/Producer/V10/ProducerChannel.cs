using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Util;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class ProducerChannel:IProducerChannel
    {
        #region event
        public event Event.BrokerAckEventHandler BrokerAcks;

        public event Event.BrokerNackEventHandler BrokerNacks;

        public event Event.CallbackExceptionEventHandler CallbackException;

        public event Event.FlowControlEventHandler FlowControl;
        #endregion 

        private ThreadSafe.Boolean _isOpen;
        public ProducerChannel(string uri, bool isReliable, bool isInOrder)
        {
            Uri = uri;
            IsReliable = isReliable;
            IsInOrder = isInOrder;

            Buffer = new ChannelBuffer();
            _isOpen = new ThreadSafe.Boolean(false);
        }

        private ChannelBuffer Buffer { get; set; }

        public IMessageProducer CreateProducer()
        {
            Open(Consts.Consumer_DefaultConnectTimeout);
            var producer = new MessageProducer(this);
            return producer;
        }

        public event Event.ChannelOutOfCapacityEventHandler OutOfCapacity;

        public System.Collections.IDictionary ClientProperties
        {
            get { return new Dictionary<string, string>(); }
        }

        public System.Collections.IDictionary ServerProperties
        {
            get { return new Dictionary<string, string>(); }
        }

        public bool IsReliable { get; private set; }

        public bool IsInOrder { get; private set; }

        public string Uri { get; private set; }

        public bool EnableMonitoring { get; set; }

        public ushort ConnectionMax { get; set; }

        public int AckTimeout { get; set; }

        public uint Capacity { get; set; }

        public uint CurrentSize { get; private set; }

        public string[] KnownHosts { get; private set; }

        public bool IsOpen
        {
            get { return _isOpen.ReadAcquireFence(); }
        }

        private bool IsHasOpen
        {
            get
            {
                return (!_isOpen.AtomicCompareExchange(true, false));
            }
        }

        public void Open(int timeout)
        {
            //已OPEN的，不再OPEN，多线程处理
            if (IsHasOpen) return;

            try
            {
  
            }
            catch
            {
                Close(timeout);
                throw;
            }
        }

        public void Close(int timeout)
        {
            _isOpen.AtomicExchange(false);
        }

        public void Dispose()
        {
            Close(Consts.Consumer_DefaultConnectTimeout);
        }

        /// <summary>
        /// 发布数据至缓存队列
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="pubMessage"></param>
        internal void PublishToBuffer(MessageProducer producer, PubMessage message )
        {

           // producer.FlowControl(producer,new FlowControlEventArgs());
          

           // Buffer.PublishMessage(producer, pubMessage);
        }
    }
}
