using System;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Freeway.Logging;
using System.Threading;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Core.Schedule;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public class DefaultConsumingStrategyConsumerTask : BaseConsumerTask
    {
        private ThreadSafe.AtomicReference<BasePullMessagesTask> pullMessagesTask = new ThreadSafe.AtomicReference<BasePullMessagesTask>(null);

        public DefaultConsumingStrategyConsumerTask(ConsumerContext context, int partitionId, int cacheSize)
            : base(context, partitionId, cacheSize)
        {
        }

        protected override void DoBeforeConsuming(ConsumerLeaseKey key, long correlationId)
        {
            ISchedulePolicy noEndpointSchedulePolicy = new ExponentialSchedulePolicy(Config.NoEndpointWaitBaseMillis, Config.NoEndpointWaitMaxMillis);
            pullMessagesTask.WriteFullFence(new DefaultPullMessagesTask(correlationId, noEndpointSchedulePolicy, this));
        }

        protected override void DoAfterConsuming(ConsumerLeaseKey key, long correlationId)
        {
            pullMessagesTask.WriteFullFence(null);
        }

        protected override BasePullMessagesTask GetPullMessageTask()
        {
            return pullMessagesTask.ReadFullFence();
        }

        public class DefaultPullMessagesTask : BasePullMessagesTask
        {

            public DefaultPullMessagesTask(long correlationId, ISchedulePolicy noEndpointSchedulePolicy, BaseConsumerTask baseConsumerTask)
                : base(correlationId, noEndpointSchedulePolicy, baseConsumerTask)
            {
            }

            public override PullMessageCommand CreatePullMessageCommand(int timeout)
            {
                return new PullMessageCommand(BaseConsumerTask.Context.Topic.Name,
                    BaseConsumerTask.PartitionId,
                    BaseConsumerTask.Context.GroupId, 
                    BaseConsumerTask.cacheSize - BaseConsumerTask.msgs.Count, 
                    BaseConsumerTask.SystemClockService.Now() + timeout + BaseConsumerTask.Config.PullMessageBrokerExpireTimeAdjustmentMills);
            }

            public override void ResultReceived(PullMessageResultCommand ack)
            {
            }
        }

    }
}

