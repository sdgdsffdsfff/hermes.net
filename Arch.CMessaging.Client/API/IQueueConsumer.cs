using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 基于Queue语义消费者接口，从<seealso cref="IMessageConsumer"/>派生。
    /// <remarks>
    /// Queue语义的消费是指，所有消费者消对指定Exchange通道的消费都是互斥的，一条消息同时只能有一个消费者消费。
    /// 强烈建议此实例应该是单例的。
    /// </remarks>
    /// <example>
    /// var channel = MessageChannelFactory.CreateChannel("http://messaging.global.sh.ctriptravel.com/");
    /// var consumer = channel.CreateConsumer<IQueueConsumer>();
    /// </example>
    /// </summary>
    public interface IQueueConsumer : IMessageConsumer
    {
        /// <summary>
        /// 绑定在Queue语义的消费管道上，如果指定的Exchange是非Queue语义的，将会接收到一个异常。
        /// </summary>
        /// <param name="exchangeName">待绑定的Exchange实例名</param>
        /// <exception cref="">QueueBindException</exception>
        void QueueBind(string exchangeName);
    }
}
