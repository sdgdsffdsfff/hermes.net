using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 当<seealso cref="IMessageProducer"/>发送消息出错，或者超时时候触发。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CallbackExceptionEventHandler(object sender, CallbackExceptionEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="CallbackExceptionEventHandler"/>所需要的参数
    /// </summary>
    public class CallbackExceptionEventArgs : EventArgs
    {
        private IDictionary detail;
        private Exception exception;
        private IMessageReader reader;

        public CallbackExceptionEventArgs(Exception exception, IMessageReader reader)
        {
            this.exception = exception;
            this.detail = new Hashtable();
            this.reader = reader;
        }

        /// <summary>
        /// 导致错误包含的信息。
        /// </summary>
        public IDictionary Detail { get { return detail; } }

        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get { return exception; } }

        /// <summary>
        /// <seealso cref="IMessageReader"/>
        /// </summary>
        public IMessageReader Reader { get { return reader; } }

        /// <summary>
        /// 是否重新发送，如果重新发送，按退避算法延迟发送。
        /// </summary>
        public bool Republish { get; set; }
    }
}
