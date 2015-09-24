using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// <see cref="IMessageChannel"/>的配置节信息
    /// </summary>
    public interface IMessageChannelConfiguration
    {
        /// <summary>
        /// 目标消息服务地址，CMessaging目前是基于Http协议开发的。
        /// </summary>
        string Uri { get; }

        /// <summary>
        /// 是否时序消息传输，在当前版本中，时序保证是通过时序号由客户端自己保证的。
        /// </summary>
        bool IsReliable { get; }

        /// <summary>
        /// 是否可靠消息传输
        /// </summary>
        bool IsInOrder { get; }
    }
}
