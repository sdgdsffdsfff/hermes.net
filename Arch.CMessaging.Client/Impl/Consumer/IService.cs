using System.Collections.Generic;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public interface IService
    {
        List<ExchangePhysicalServer> GetExchangePhysicalServers(string identitys, int timeout);
        SubChunk Pulling(PhysicalServer server, int timeout, PullingRequest request);
        ChunkAck Ack(PhysicalServer server, int timeout, ConsumerAckChunk chunk);
    }
}
