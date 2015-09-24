using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Impl.Consumer;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public sealed class MessageChannelConfigurator : IMessageChannelConfigurator
    {
        public void Configure(IMessageChannel channel)
        {

        }

        public IMessageChannelConfiguration GetConfiguration()
        {
            return DefaultMessageChannelConfiguration.Instance;
        }
    }
}
