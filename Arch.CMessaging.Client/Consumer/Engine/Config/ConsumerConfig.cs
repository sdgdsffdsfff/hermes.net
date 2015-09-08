using System;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Env;

namespace Arch.CMessaging.Client.Consumer.Engine.Config
{
    [Named(ServiceType = typeof(ConsumerConfig))]
    public class ConsumerConfig
    {
        public const int DEFAULT_LOCALCACHE_SIZE = 10;

        [Inject]
        private IClientEnvironment clientEnv;

        public int GetLocalCacheSize(String topic)
        {
            string localCacheSizeStr = clientEnv.GetConsumerConfig(topic).GetProperty("consumer.localcache.size");
            if (string.IsNullOrWhiteSpace(localCacheSizeStr))
            {
                return DEFAULT_LOCALCACHE_SIZE;
            }
            else
            {
                return Convert.ToInt32(localCacheSizeStr);
            }
        }

        public long RenewLeaseTimeMillisBeforeExpired
        { 
            get { return 5 * 1000L; }
        }

        public long StopConsumerTimeMillsBeforLeaseExpired
        { 
            get { return RenewLeaseTimeMillisBeforeExpired - 3 * 1000L; }
        }

        public long DefaultLeaseAcquireDelayMillis
        { 
            get { return 500L; }
        }

        public long DefaultLeaseRenewDelayMillis
        { 
            get { return 500L; }
        }

        public int NoMessageWaitBaseMillis
        {
            get { return 50; }
        }

        public int NoMessageWaitMaxMillis
        {
            get { return 800; }
        }

        public int NoEndpointWaitBaseMillis
        {
            get { return 500; }
        }

        public int NoEndpointWaitMaxMillis
        {
            get { return 4000; }
        }

        public int PullMessageBrokerExpireTimeAdjustmentMills
        {
            get { return -500; }
        }

        public int QueryOffsetTimeoutMillis
        {
            get { return 3000; }
        }

        public String DefaultNotifierThreadCount
        { 
            get { return "1"; }
        }
    }
}

