﻿using System;
using Arch.CMessaging.Client.Core.Bo;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Core.MetaService;

namespace Arch.CMessaging.Client.Consumer.Engine.Lease
{
	public class ConsumerLeaseManager : ILeaseManager<ConsumerLeaseKey>
	{
		private IMetaService MetaService;

		public LeaseAcquireResponse tryAcquireLease (ConsumerLeaseKey key)
		{
			return MetaService.TryAcquireConsumerLease (key.Tpg, key.SessionId);
		}

		public LeaseAcquireResponse tryRenewLease (ConsumerLeaseKey key, ILease lease)
		{
			return MetaService.TryRenewConsumerLease (key.Tpg, lease, key.GetSessionId ());
		}
	}

	public class ConsumerLeaseKey : ISessionIdAware
	{
		public Tpg Tpg { get; }

		public String SessionId { get; }

		public ConsumerLeaseKey (Tpg tpg, String sessionId)
		{
			this.Tpg = tpg;
			SessionId = sessionId;
		}

		public String GetSessionId ()
		{
			return SessionId;
		}

		public override int GetHashCode ()
		{
			int prime = 31;
			int result = 1;
			result = prime * result + ((SessionId == null) ? 0 : SessionId.GetHashCode ());
			result = prime * result + ((Tpg == null) ? 0 : Tpg.GetHashCode ());
			return result;
		}

		public  override bool Equals (Object obj)
		{
			if (this == obj)
				return true;
			if (obj == null)
				return false;
			if (GetType() != obj.GetType())
				return false;
			ConsumerLeaseKey other = (ConsumerLeaseKey)obj;
			if (SessionId == null) {
				if (other.SessionId != null)
					return false;
			} else if (!SessionId.Equals (other.SessionId))
				return false;
			if (Tpg == null) {
				if (other.Tpg != null)
					return false;
			} else if (Tpg.Equals (other.Tpg))
				return false;
			return true;
		}

	}
}
