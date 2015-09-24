using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    /// <summary>
    /// 管道消息处理
    /// </summary>
    interface IChannelBuffer
    {
        void PublishMessage(string identifier, PubMessage message);
    }
}
