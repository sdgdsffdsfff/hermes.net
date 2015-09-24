using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class DefaultMessageChannelConfigurator: IMessageChannelConfigurator
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
