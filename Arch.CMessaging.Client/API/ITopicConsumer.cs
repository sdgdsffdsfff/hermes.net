using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 基于Topic语义消费者接口，从<seealso cref="IMessageConsumer"/>派生。
    /// <remarks>
    /// Topic语义实质上是pub/sub模式的一种，不同消费者可以共享消费Exchange通道任何一条消息，批次互不干扰，
    /// 但是同一个消费者，尽管可以是集群的，但是消费一定是互斥的。
    /// Topic是消费者用来订阅消费主题的。含两种模式：
    /// 1: Direct. 字符串完全匹配。
    /// 2: Fuzzy. 包含多个word，用”.” 分割， * 匹配一个word, # 匹配0或多个。
    /// <example>
    /// ->subject: A.B.C.D
    /// ->topic: A.B.C.*    能够订阅
    /// ->topic: A.B.*      不能订阅
    /// ->topic: A.#        能够订阅
    /// </example>
    /// </remarks>
    /// <example>
    /// var channel = MessageChannelFactory.CreateChannel("http://messaging.global.sh.ctriptravel.com/");
    /// var consumer = channel.CreateConsumer<ITopicConsumer>();
    /// </example>
    /// </summary>
    public interface ITopicConsumer : IMessageConsumer
    {
        /// <summary>
        /// 设置基于Header过滤的条件，格式必须是{0}:{1}
        /// <example>
        /// Content-Type:image/gif
        /// </example>
        /// </summary>
        string HeaderFilter { get; }

        /// <summary>
        /// 绑定在Topic语义的消费管道上，如果指定的Exchange是非Topic语义的，将会接收到一个异常。
        /// </summary>
        /// <param name="topic">订阅主题</param>
        /// <param name="exchangeName">绑定Exchange实例</param>
        /// <param name="queueName">
        /// 在Broker内部会生成一个消息队列，是Consumer消费的管道。
        /// 这个名字在监控的时候起作用，如果发生队列阻塞，可以跟踪到时哪个应用。
        /// 如果不指定，系统自动生成。
        /// </param>
        /// <exception cref="">TopicBindException</exception>
        void TopicBind(
            string topic,
            string exchangeName,
            string queueName = null);
    }
}
