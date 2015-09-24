using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Event
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void AckingEventHandler(object sender, AckingEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="CallbackExceptionEventHandler"/>所需要的参数
    /// </summary>
    public class AckingEventArgs : EventArgs
    {
        public string Uri { get; set; }
        public ConsumerAckChunk AckChunk { get; set; }

        public AckingEventArgs(string uri, ConsumerAckChunk chunk)
        {
            Uri = uri;
            AckChunk = chunk;
        }
    }
}
