using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 已配置的方式初始化<see cref="IMessageChannel"/>
    /// </summary>
    public interface IMessageChannelConfigurator
    {
        /// <summary>
        /// 对<see cref="IMessageChannel"/>进行配置
        /// </summary>
        /// <param name="channel"><seealso cref="IMessageChannel"/></param>
        void Configure(IMessageChannel channel);

        /// <summary>
        /// 获取配置节<see cref="IMessageChannelConfiguration"/>对象
        /// </summary>
        /// <returns><see cref="IMessageChannelConfiguration"/></returns>
        IMessageChannelConfiguration GetConfiguration();
    }
}
