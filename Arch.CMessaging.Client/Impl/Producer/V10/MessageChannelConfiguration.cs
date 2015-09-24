using Arch.CFramework.AppInternals.Configuration.Bean;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Impl.Producer
{
    internal sealed class MessageChannelConfiguration: ConfigBeanBase, IMessageChannelConfiguration
    {
        public MessageChannelConfiguration() : base(true) { }

        public string Uri
        {
            get;
            private set;
        }

        public bool IsReliable
        {
            get;
            private set;
        }

        public bool IsInOrder
        {
            get;
            private set;
        }

        protected override void Load()
        {
            Uri = Consts.Producer_ExchangeServiceUrl;
            IsReliable = false;
            IsInOrder = false;
        }

        private static MessageChannelConfiguration _instance;
        private static readonly object LockObject = new object();
        public static MessageChannelConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = ConfigBeanManager.Current.GetConfigBean(typeof(MessageChannelConfiguration).FullName) as MessageChannelConfiguration;
                            if (_instance == null)
                            {
                                _instance = new MessageChannelConfiguration();
                                ConfigBeanManager.Current.Register(_instance);
                            }
                        }
                    }
                }
                return _instance;
            }
        }
    }
}
