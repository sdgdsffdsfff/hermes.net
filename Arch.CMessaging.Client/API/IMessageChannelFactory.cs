using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 消息发送通道工厂接口。
    /// </summary>
    public interface IMessageChannelFactory
    {
        /// <summary>
        /// 生产一个消费发送通道，基于配置文件
        /// </summary>
        /// <param name="configurator"><seealso cref="IMessageChannelConfigurator"/></param>
        /// <returns><seealso cref="IMessageChannel"/></returns>
        TChannel CreateChannel<TChannel>(IMessageChannelConfigurator configurator)
            where TChannel : IMessageChannel;

        /// <summary>
        /// 生产一个消费发送通道，基于参数
        /// </summary>
        /// <param name="uri">Broker服务地址</param>
        /// <param name="reliable">是否可靠通道</param>
        /// <param name="inOrder">是否是时序通道</param>
        /// <returns><seealso cref="IMessageChannel"/></returns>
        TChannel CreateChannel<TChannel>(string uri, bool reliable = false, bool inOrder = false)
            where TChannel : IMessageChannel;
    }
}
