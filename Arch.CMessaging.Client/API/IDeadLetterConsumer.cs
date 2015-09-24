using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 死信消费者接口，从<seealso cref="IMessageConsumer"/>派生。
    /// <example>
    /// var channel = MessageChannelFactory.CreateChannel("http://messaging.global.sh.ctriptravel.com/");
    /// var consumer = channel.CreateConsumer<IDeadLetterConsumer>();
    /// </example>
    /// </summary>
    public interface IDeadLetterConsumer : IMessageConsumer
    {
        /// <summary>
        /// 绑定在一个死信通道上
        /// </summary>
        /// <param name="exchangeName">exchange的名称</param>
        /// <param name="topic">死信主题，如果为空或*，以Queue语义处理死信消息</param>
        void DeadLetterBind(string exchangeName, string topic = null);
    }
}
