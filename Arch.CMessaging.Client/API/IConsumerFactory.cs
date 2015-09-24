using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 用于快速生成Consumer的工厂
    /// <remarks>
    /// 这是一个简化的过程，如果需要处理异常，内存控制，限流等操作
    /// 请参考使用<see cref="IMessageChannelFactory"/>
    /// </remarks>
    /// </summary>
    public interface IConsumerFactory 
    {
        /// <summary>
        /// 快速生成一个基于Topic语义消费的Consumer
        /// </summary>
        /// <param name="topic">订阅主题</param>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">标识Consumer的身份</param>
        /// <returns><see cref="ITopicConsumer"/></returns>
        ITopicConsumer CreateAsTopic(string topic, string exchangeName, string identifier);

        /// <summary>
        /// 快速生成一个基于Queue语义消费的Consumer
        /// </summary>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">标识Consumer的身份</param>
        /// <returns><see cref="IQueueConsumer"/></returns>
        IQueueConsumer CreateAsQueue(string exchangeName, string identifier);
    }
}
