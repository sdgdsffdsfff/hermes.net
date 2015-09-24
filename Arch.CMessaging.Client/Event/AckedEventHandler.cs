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
    public delegate void AckedEventHandler(object sender, AckedEventArgs e);

    /// <summary>
    /// 包含必要的处理事件<seealso cref="CallbackExceptionEventHandler"/>所需要的参数
    /// </summary>
    public class AckedEventArgs : EventArgs
    {
        public ChunkAck ChunkAck { get; set; }
        public ConsumerAckChunk AckChunk { get; set; }

        public AckedEventArgs(ConsumerAckChunk chunk, ChunkAck ack)
        {
            ChunkAck = ack;
            AckChunk = chunk;
        }
    }
}
