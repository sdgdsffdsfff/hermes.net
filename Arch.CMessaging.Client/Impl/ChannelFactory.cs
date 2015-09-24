using System;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl
{
    public class ChannelFactory : IMessageChannelFactory
    {
        public TChannel CreateChannel<TChannel>(IMessageChannelConfigurator configurator) where TChannel : IMessageChannel
        {
            Guard.ArgumentNotNull(configurator, "configurator");
            //if (configurator == null)
            //{
            //    configurator = new DefaultMessageChannelConfigurator();
            //}
            var configuration = configurator.GetConfiguration(); //?? DefaultMessageChannelConfiguration.Instance;
            if(configuration == null) throw new NullReferenceException("configuration is null");
            var channel = CreateChannel<TChannel>(configuration.Uri, configuration.IsReliable, configuration.IsInOrder);
            configurator.Configure(channel);
            return channel;
        }

        public TChannel CreateChannel<TChannel>(string uri, bool reliable = false, bool inOrder = false) where TChannel : IMessageChannel
        {
            return (TChannel)Activator.CreateInstance(typeof(TChannel), uri, reliable, inOrder);
        }
    }
}
