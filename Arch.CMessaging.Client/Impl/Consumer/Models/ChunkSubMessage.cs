using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Consumer.Models
{
    /// <summary>
    /// 继承于SubMessage,增加了Chunk上的两个字段 ServerHostName,Timestamp,用于ACK时查找消息来源DISPATCHER
    /// </summary>
    public class ChunkSubMessage:SubMessage
    {
        public string ServerHostName { get; set; }

        public long Timestamp { get; set; }
    }
}
