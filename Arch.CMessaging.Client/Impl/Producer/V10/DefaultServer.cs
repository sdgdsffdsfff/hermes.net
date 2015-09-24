using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class DefaultServer : IServer
    {
        public DefaultServer()
        {
            Client = new DefaultClient(); 
        }

        public IClient Client { get;private set;}

        public ChunkAck Publish(PubChunk chunk,string exchangeServiceUrl,int timeout)
        {
            Guard.ArgumentNotNullOrEmpty(exchangeServiceUrl, "exchangeServiceUrl");
    
            var client = Client.GetClient(exchangeServiceUrl, timeout);
            return client.Publish(chunk);
        }
    }
}
