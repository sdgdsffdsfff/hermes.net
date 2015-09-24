using System;
using Arch.CMessaging.Core.Transmit.Thrift.Protocol;
using Arch.CMessaging.Core.Transmit.Thrift.Transport;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class DefaultClient:IClient
    {
        public ExchangeServiceWrapper.Client GetClient(string uri,int timeout)
        {
            var transport = new THttpClient(new Uri(uri));
            if (timeout > 0) transport.ConnectTimeout = timeout;
            var protocol = new TBinaryProtocol(transport);
            return new ExchangeServiceWrapper.Client(protocol);
        }
    }
}
