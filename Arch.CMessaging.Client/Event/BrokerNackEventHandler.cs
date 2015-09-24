using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// Broker拒绝接收事件，通知消息被拒绝。
    /// </summary>
    /// <param name="producer"><seealso cref="IMessageProducer"/></param>
    /// <param name="e"><seealso cref="BrokerNackEventArgs"/></param>
    public delegate void BrokerNackEventHandler(IMessageProducer producer, BrokerNackEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="BrokerNackEventHandler"/>所需要的参数
    /// </summary>
    public class BrokerNackEventArgs : EventArgs
    {
        private IMessageReader reader;
        private IHeaderProperties properties;
        public BrokerNackEventArgs(IMessageReader reader, IHeaderProperties properties)
        {
            this.reader = reader;
            this.properties = properties;
        }

        /// <summary>
        /// <seealso cref="IMessageReader"/>
        /// </summary>
        public IMessageReader MessageReader { get { return reader; } }

        /// <summary>
        /// 是否重新发送消息，如果重新发送，按退避算法延迟发送。
        /// </summary>
        public bool Republish { get; set; }
    }
}
