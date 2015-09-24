using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    public interface IConsumerChannel : IMessageChannel
    {
        /// <summary>
        /// 在通道上生成一个新的消息消费者接口<see cref="IMessageConsumer"/>。
        /// </summary>
        /// <typeparam name="TMessageConsumer">消费者接口的类型</typeparam>
        /// <returns><seealso cref="IMessageConsumer"/></returns>
        TMessageConsumer CreateConsumer<TMessageConsumer>()
            where TMessageConsumer : IMessageConsumer;
    }
}
