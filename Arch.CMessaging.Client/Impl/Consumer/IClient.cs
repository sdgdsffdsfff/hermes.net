using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public interface IClient
    {
        DispatcherServiceWrapper.Client CreateDispatcherServiceClient(string uri, int timeout);
    }
}
