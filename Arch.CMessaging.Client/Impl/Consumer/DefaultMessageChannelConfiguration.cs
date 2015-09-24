using Arch.CFramework.AppInternals.Configuration.Bean;
using Arch.CMessaging.Client.API;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class DefaultMessageChannelConfiguration : ConfigBeanBase, IMessageChannelConfiguration
    {
        public DefaultMessageChannelConfiguration() : base(true) { }

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
            Uri = "";
            IsReliable = false;
            IsInOrder = false;
        }

        private static DefaultMessageChannelConfiguration _instance;
        private static readonly object LockObject = new object();
        public static DefaultMessageChannelConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (LockObject)
                    {
                        if (_instance == null)
                        {
                            _instance = ConfigBeanManager.Current.GetConfigBean(typeof(DefaultMessageChannelConfiguration).FullName) as DefaultMessageChannelConfiguration;
                            if (_instance == null)
                            {
                                _instance = new DefaultMessageChannelConfiguration();
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
