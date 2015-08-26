using System;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Config
{
    [Named(ServiceType = typeof(ConsumerConfig))]
    public class ConsumerConfig
    {
        public String DefautlLocalCacheSize
        { 
            get { return "10"; }
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

        public int NoMessageWaitIntervalMillis
        {
            get { return 50; }
        }

        public int NoEndpointWaitIntervalMillis
        {
            get { return 500; }
        }

        public String DefaultNotifierThreadCount
        { 
            get { return "1"; }
        }
    }
}

