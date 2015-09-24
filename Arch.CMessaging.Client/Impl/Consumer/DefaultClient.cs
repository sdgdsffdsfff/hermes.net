using System;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Transmit.Thrift.Protocol;
using Arch.CMessaging.Core.Transmit.Thrift.Transport;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    /// <summary>
    /// 通信方式
    /// </summary>
    public class DefaultClient:IClient
    {
        public DispatcherServiceWrapper.Client CreateDispatcherServiceClient(string uri, int timeout)
        {
            var transport = new THttpClient(new Uri(uri));
            var time = ConfigUtil.Instance.Timeout;
            if (time > 0)
            {
                transport.ConnectTimeout = time;
                transport.ReadTimeout = time;
            }
            var protocol = new TBinaryProtocol(transport);
            return new DispatcherServiceWrapper.Client(protocol);
        }
    }
}
