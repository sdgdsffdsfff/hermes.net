using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// Broker确认事件，通知消息已经被确认。
    /// </summary>
    /// <param name="producer"><seealso cref="IMessageProducer"/></param>
    /// <param name="e"><seealso cref="BrokerAckEventArgs"/></param>
    public delegate void BrokerAckEventHandler(IMessageProducer producer, BrokerAckEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="BrokerAckEventHandler"/>所需要的参数
    /// </summary>
    public class BrokerAckEventArgs : EventArgs
    {
        private IMessageReader reader;
        private IHeaderProperties properties;
        public BrokerAckEventArgs(IMessageReader reader, IHeaderProperties properties)
        {
            this.reader = reader;
            this.properties = properties;
        }

        /// <summary>
        /// <seealso cref="IMessageReader"/>
        /// </summary>
        public IMessageReader MessageReader { get { return reader; } }
    }
}
