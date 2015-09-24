using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 用于快速生成Producer的工厂
    /// <remarks>
    /// 这是一个简化的过程，如果需要处理异常，内存控制，限流等操作
    /// 请参考使用<see cref="IMessageChannelFactory"/>
    /// </remarks>
    /// </summary>
    public interface IProducerFactory
    {
        /// <summary>
        /// 快速创建一个简化的Producer用于发送消息,
        /// 创建的过程将使用默认值初始化<see cref="IMessageChannelFactory"/>，
        /// 此外在调用方法PublishAsync，发生OutOfCapacity异常的时候，异常将直接抛出，
        /// 调用者需要自行完成消息处理逻辑，否则会引起消息丢失。
        /// 如果需要避免此类情况，请参考使用<see cref="IMessageChannelFactory"/>生成。
        /// </summary>
        /// <param name="exchangeName">Exchange实例名</param>
        /// <param name="identifier">发送者的身份</param>
        /// <returns><see cref="IMessageProducer"/></returns>
        IMessageProducer Create(string exchangeName, string identifier);
    }
}
