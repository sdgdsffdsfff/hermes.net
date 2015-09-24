using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 当超过<seealso cref="IMessageChannel"/>能够处理的消息总容量的时候，触发。
    /// </summary>
    /// <param name="channel"><seealso cref="IMessageChannel"/></param>
    /// <param name="e"><seealso cref="OutOfCapacityEventArgs"/></param>
    public delegate void ChannelOutOfCapacityEventHandler(IMessageChannel channel, OutOfCapacityEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="ChannelOutOfCapacityEventHandler"/>所需要的参数
    /// </summary>
    public class OutOfCapacityEventArgs : EventArgs
    {
        /// <summary>
        /// 指定当容量溢出之后，是否对调用线程抛出异常，如果并发请求数很大，抛出异常而不是等待，能够减少服务器压力。
        /// </summary>
        public bool RaiseError { get; set; }

        /// <summary>
        /// 指定当容量溢出之后，是否让调用线程一直处于阻塞等待状态，直到接收到信号通知。
        /// </summary>
        public bool Wait { get; set; }

        /// <summary>
        /// 等待超时，超时之后，会强制抛出异常。
        /// </summary>
        public int WaitTimeout { get; set; }
    }
}
