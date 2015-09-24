using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Bo;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Core.Message.Retry;
using Arch.CMessaging.Client.MetaEntity.Entity;

namespace Arch.CMessaging.Client.Core.MetaService
{
    public interface IMetaService
    {
        string FindAvroSchemaRegistryUrl();
        Codec FindCodecByTopic(string topic);
        Endpoint FindEndpointByTopicAndPartition(string topic, int partition);
        string FindEndpointTypeByTopic(string topic);
        Partition FindPartitionByTopicAndPartition(string topic, int partition);
        IRetryPolicy FindRetryPolicyByTopicAndGroup(string topic, string groupId);
        Storage FindStorageByTopic(string topic);
        Topic FindTopicByName(string topic);
        int GetAckTimeoutSecondsTopicAndConsumerGroup(string topic, string groupId);
        List<Partition> ListPartitionsByTopic(string topic);
        List<Topic> ListTopicsByPattern(string topicPattern);
        LeaseAcquireResponse TryRenewBrokerLease(string topic, int partition, ILease lease, string sessionId, int brokerPort);
        int TranslateToIntGroupId(string topic, string groupId);
        LeaseAcquireResponse TryAcquireBrokerLease(string topic, int partition, string sessionId, int brokerPort);
        LeaseAcquireResponse TryAcquireConsumerLease(Tpg tpg, string sessionId);
        LeaseAcquireResponse TryRenewConsumerLease(Tpg tpg, ILease lease, string sessionId);
        List<SubscriptionView> ListSubscriptions();
        List<SchemaView> ListSchemas();
        bool ContainsEndpoint(Endpoint endpoint);
        bool ContainsConsumerGroup(string topicName, string groupId);
    }
}
