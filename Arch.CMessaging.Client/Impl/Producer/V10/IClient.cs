using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public interface IClient
    {
        ExchangeServiceWrapper.Client GetClient(string exchangeServiceUrl, int timeout);
    }
}
