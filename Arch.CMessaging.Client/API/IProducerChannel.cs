using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    public interface IProducerChannel : IMessageChannel
    {
        /// <summary>
        /// 在通道上生成一个新的消息生产者接口<see cref="IMessageProducer"/>。
        /// </summary>
        /// <returns><see cref="IMessageProducer"/></returns>
        IMessageProducer CreateProducer();
    }
}
