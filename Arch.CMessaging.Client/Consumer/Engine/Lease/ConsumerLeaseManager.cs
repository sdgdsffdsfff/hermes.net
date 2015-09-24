using System;
using Arch.CMessaging.Client.Core.Bo;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Core.MetaService;
using Arch.CMessaging.Client.Consumer.Build;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Lease
{
    [Named(ServiceType = typeof(ILeaseManager<ConsumerLeaseKey>), ServiceName = BuildConstants.CONSUMER)]
    public class ConsumerLeaseManager : ILeaseManager<ConsumerLeaseKey>
    {
        [Inject]
        private IMetaService MetaService;

        public LeaseAcquireResponse TryAcquireLease(ConsumerLeaseKey key)
        {
            return MetaService.TryAcquireConsumerLease(key.Tpg, key.SessionId);
        }

        public LeaseAcquireResponse TryRenewLease(ConsumerLeaseKey key, ILease lease)
        {
            return MetaService.TryRenewConsumerLease(key.Tpg, lease, key.GetSessionId());
        }
    }

}

