using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 在流控的时候触发
    /// </summary>
    /// <param name="producer"><seealso cref="IMessageProducer"/></param>
    /// <param name="args"><seealso cref="FlowControlEventArgs"/></param>
    public delegate void FlowControlEventHandler(IMessageProducer producer, FlowControlEventArgs args);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="FlowControlEventHandler"/>所需要的参数
    /// </summary>
    public class FlowControlEventArgs : EventArgs
    {
    }
}
