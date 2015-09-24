using System;
using Freeway.Logging;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Core.Schedule;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Core.Bo;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public class StrictlyOrderedConsumingStrategyConsumerTask : BaseConsumerTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(StrictlyOrderedConsumingStrategyConsumerTask));

        private ThreadSafe.AtomicReference<PullMessagesTask> pullMessagesTask = new ThreadSafe.AtomicReference<PullMessagesTask>(null);

        private IQueryOffsetResultMonitor queryOffsetResultMonitor;

        private ISchedulePolicy noEndpointSchedulePolicy;

        public ThreadSafe.AtomicReference<Offset> offset = new ThreadSafe.AtomicReference<Offset>(null);

        public StrictlyOrderedConsumingStrategyConsumerTask(ConsumerContext context, int partitionId, int cacheSize)
            : base(context, partitionId, cacheSize)
        {
            queryOffsetResultMonitor = ComponentLocator.Lookup<IQueryOffsetResultMonitor>();
        }

        protected override void DoBeforeConsuming(ConsumerLeaseKey key, long correlationId)
        {
            noEndpointSchedulePolicy = new ExponentialSchedulePolicy(Config.NoEndpointWaitBaseMillis,
                Config.NoEndpointWaitMaxMillis);

            QueryLatestOffset(key, correlationId);
            // reset policy
            noEndpointSchedulePolicy.Succeess();

            pullMessagesTask.WriteFullFence(new PullMessagesTask(correlationId, noEndpointSchedulePolicy, this));
        }

        protected override BrokerConsumerMessage DecorateBrokerMessage(BrokerConsumerMessage brokerMsg)
        {
            brokerMsg.AckWithForwardOnly = true;
            return brokerMsg;
        }

        private void QueryLatestOffset(ConsumerLeaseKey key, long correlationId)
        {

            while (!IsClosed() && !leaseRef.ReadFullFence().Expired)
            {

                Endpoint endpoint = EndpointManager.GetEndpoint(Context.Topic.Name, PartitionId);
                if (endpoint == null)
                {
                    log.Warn(string.Format("No endpoint found for topic {0} partition {1}, will retry later", Context.Topic.Name, PartitionId));
                    noEndpointSchedulePolicy.Fail(true);
                    continue;
                }
                else
                {
                    noEndpointSchedulePolicy.Succeess();
                }

                SettableFuture<QueryOffsetResultCommand> future = SettableFuture<QueryOffsetResultCommand>.Create();

                QueryOffsetCommand cmd = new QueryOffsetCommand(Context.Topic.Name, PartitionId, Context.GroupId);

                cmd.Header.CorrelationId = correlationId;
                cmd.Future = future;

                QueryOffsetResultCommand offsetRes = null;

                int timeout = Config.QueryOffsetTimeoutMillis;

                queryOffsetResultMonitor.Monitor(cmd);
                EndpointClient.WriteCommand(endpoint, cmd, timeout);

                try
                {
                    offsetRes = future.Get(timeout);
                }
                catch (Exception e)
                {
                }
                finally
                {
                    queryOffsetResultMonitor.Remove(cmd);
                }

                if (offsetRes != null && offsetRes.Offset != null)
                {
                    offset.WriteFullFence(offsetRes.Offset);
                    return;
                }
                else
                {
                    noEndpointSchedulePolicy.Fail(true);
                }

            }
        }

        protected override void DoAfterConsuming(ConsumerLeaseKey key, long correlationId)
        {
            pullMessagesTask.WriteFullFence(null);
            offset.WriteFullFence(null);
        }

        protected override BasePullMessagesTask GetPullMessageTask()
        {
            return pullMessagesTask.ReadFullFence();
        }

        private class PullMessagesTask : BasePullMessagesTask
        {

            private StrictlyOrderedConsumingStrategyConsumerTask strictlyOrderedConsumingStrategyConsumerTask;

            public PullMessagesTask(long correlationId, ISchedulePolicy noEndpointSchedulePolicy, StrictlyOrderedConsumingStrategyConsumerTask baseConsumerTask)
                : base(correlationId, noEndpointSchedulePolicy, baseConsumerTask)
            {
                strictlyOrderedConsumingStrategyConsumerTask = baseConsumerTask;
            }

            public override void ResultReceived(PullMessageResultCommandV2 ack)
            {
                if (ack.Offset != null)
                {
                    strictlyOrderedConsumingStrategyConsumerTask.offset.WriteFullFence(ack.Offset);
                }
            }

            public override PullMessageCommandV2 CreatePullMessageCommand(int timeout)
            {
                return new PullMessageCommandV2(PullMessageCommandV2.PULL_WITH_OFFSET, BaseConsumerTask.Context.Topic.Name,
                    BaseConsumerTask.PartitionId, BaseConsumerTask.Context.GroupId, strictlyOrderedConsumingStrategyConsumerTask.offset.ReadFullFence(), BaseConsumerTask.msgs.RemainingCapacity,
                    BaseConsumerTask.SystemClockService.Now() + timeout + BaseConsumerTask.Config.PullMessageBrokerExpireTimeAdjustmentMills);
            }

        }


    }
}

