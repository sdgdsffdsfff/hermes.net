using System;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class DefaultMessageChannelFactory : IMessageChannelFactory
    {
        public static DefaultMessageChannelFactory Instance = new DefaultMessageChannelFactory();

        internal DefaultMessageChannelFactory()
        {
        }

        public TChannel CreateChannel<TChannel>(IMessageChannelConfigurator configurator) where TChannel : IMessageChannel
        {
            throw new NotImplementedException();
        }

        public TChannel CreateChannel<TChannel>(string uri, bool reliable = false, bool inOrder = false) where TChannel : IMessageChannel
        {
            return (TChannel)Activator.CreateInstance(typeof(DefaultMessageChannel));
        }
    }
}
