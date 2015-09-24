using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 当<seealso cref="IMessageConsumer"/>消费消息出错，或者超时时候触发。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ConsumeExceptionEventHandler(object sender, ConsumeExceptionEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="ConsumeExceptionEventHandler"/>所需要的参数
    /// </summary>
    public class ConsumeExceptionEventArgs : EventArgs
    {
        public IMessageConsumer Consumer { get; private set; }
        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get; private set; }

        public ConsumeExceptionEventArgs(Exception exception, IMessageConsumer consumer)
        {
            Consumer = consumer;
            Exception = exception;
        }
    }

}
