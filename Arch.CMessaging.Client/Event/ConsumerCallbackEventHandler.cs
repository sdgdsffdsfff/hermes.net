using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Impl.Consumer;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 当收到消息的时候触发。
    /// </summary>
    /// <param name="consumer"><seealso cref="IMessageConsumer"/></param>
    /// <param name="e"><seealso cref="ConsumerCallbackEventArgs"/></param>
    public delegate void ConsumerCallbackEventHandler(IMessageConsumer consumer, ConsumerCallbackEventArgs e);

    /// <summary>包含必要的处理事件<seealso cref="ConsumerCallbackEventHandler"/>所需要的参数
    /// 
    /// </summary>
    public class ConsumerCallbackEventArgs : EventArgs
    {
        private IMessageReader reader;
        public ConsumerCallbackEventArgs(IMessageReader reader)
        {
            this.reader = reader;
        }

        public IMessage Message { get; internal set; }

        /// <summary>
        /// <seealso cref="MessageReader"/>
        /// </summary>
        [Obsolete]
        public IMessageReader MessageReader { get { return reader; } }

        private AckMode ackMode;
        /// <summary>
        /// 是否确认消息，当处理中有异常，请确认方法最后执行AckMode.Nack，
        /// 否则消息将执行确认操作。
        /// <example>
        /// try
        /// {
        /// }
        /// catch(Exception)
        /// {
        ///     Acks = AckMode.Nack;
        /// }
        /// </example>
        /// </summary>
        [Obsolete]
        public AckMode Acks
        {
            get { return ackMode; }
            set
            {
                ackMode = value;
                if (Message != null) Message.Acks = value;
            }
        }
    }
}
