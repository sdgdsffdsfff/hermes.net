using System;
using Arch.CMessaging.Client.Consumer.Engine.Notifier;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Transport.EndPoint;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Consumer.Engine;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Consumer.Engine.Config;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Core.Bo;
using Freeway.Logging;
using System.Threading;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.MetaEntity.Entity;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Core.Message.Retry;
using Arch.CMessaging.Client.Consumer.Build;
using Arch.CMessaging.Client.Core.MetaService;
using Arch.CMessaging.Client.Core.Schedule;
using Com.Dianping.Cat;
using Com.Dianping.Cat.Message;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public abstract class BaseConsumerTask : IConsumerTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(BaseConsumerTask));

        public IConsumerNotifier ConsumerNotifier { get; set; }

        public IMessageCodec MessageCodec { get; set; }

        public IEndpointManager EndpointManager { get; set; }

        public IEndpointClient EndpointClient { get; set; }

        public ILeaseManager<ConsumerLeaseKey> LeaseManager { get; set; }

        public ISystemClockService SystemClockService { get; set; }

        public ConsumerConfig Config{ get; set; }

        public ProducerConsumer<BasePullMessagesTask> pullMessageTaskExecutor;

        public TimeoutNotifyProducerConsumer<RenewLeaseTask> renewLeaseTaskExecutor;

        public IPullMessageResultMonitor PullMessageResultMonitor { get; set; }

        public BlockingQueue<IConsumerMessage> msgs;

        public ConsumerContext Context;

        public int PartitionId;

        public ThreadSafe.Boolean pullTaskRunning = new ThreadSafe.Boolean(false);

        public ThreadSafe.AtomicReference<ILease> leaseRef = new ThreadSafe.AtomicReference<ILease>(null);

        public volatile bool closed = false;

        public IRetryPolicy retryPolicy;

        public ThreadSafe.Integer scheduleKey = new ThreadSafe.Integer(0);

        public BaseConsumerTask(ConsumerContext context, int partitionId, int localCacheSize)
        {
            Context = context;
            PartitionId = partitionId;
            msgs = new BlockingQueue<IConsumerMessage>(localCacheSize);

            LeaseManager = ComponentLocator.Lookup<ILeaseManager<ConsumerLeaseKey>>(BuildConstants.CONSUMER);
            ConsumerNotifier = ComponentLocator.Lookup<IConsumerNotifier>();
            EndpointClient = ComponentLocator.Lookup<IEndpointClient>();
            EndpointManager = ComponentLocator.Lookup<IEndpointManager>();
            MessageCodec = ComponentLocator.Lookup<IMessageCodec>();
            SystemClockService = ComponentLocator.Lookup<ISystemClockService>();
            Config = ComponentLocator.Lookup<ConsumerConfig>();
            retryPolicy = ComponentLocator.Lookup<IMetaService>().FindRetryPolicyByTopicAndGroup(
                context.Topic.Name, context.GroupId);
            PullMessageResultMonitor = ComponentLocator.Lookup<IPullMessageResultMonitor>();

            pullMessageTaskExecutor = new ProducerConsumer<BasePullMessagesTask>(int.MaxValue);
            pullMessageTaskExecutor.OnConsume += RunPullMessageTask;

            renewLeaseTaskExecutor = new TimeoutNotifyProducerConsumer<RenewLeaseTask>(int.MaxValue);
            renewLeaseTaskExecutor.OnConsume += RunRenewLeaseTask;
        }

        private void RunRenewLeaseTask(object sender, ConsumeEventArgs e)
        {
            RenewLeaseTask[] tasks = (e.ConsumingItem as ChunkedConsumingItem<RenewLeaseTask>).Chunk;
            if (tasks != null && tasks.Length > 0)
            {
                foreach (var task in tasks)
                {
                    RenewLeaseTaskRun(task);
                }
            }
        }

        private void RunPullMessageTask(object sender, ConsumeEventArgs e)
        {
            BasePullMessagesTask task = (e.ConsumingItem as SingleConsumingItem<BasePullMessagesTask>).Item;
            PullMessagesTaskRun(task);
        }

        protected bool IsClosed()
        {
            return closed;
        }

        public void Start()
        {
            log.Info(string.Format("Consumer started(mode={0}, topic={1}, partition={2}, groupId={3}, sessionId={4})",
                    Context.ConsumerType, Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId));
            ConsumerLeaseKey key = new ConsumerLeaseKey(new Tpg(Context.Topic.Name, PartitionId,
                                           Context.GroupId), Context.SessionId);
            while (!IsClosed())
            {
                try
                {
                    
                    AcquireLease(key);

                    if (!IsClosed() && leaseRef.ReadFullFence() != null && !leaseRef.ReadFullFence().Expired)
                    {
                        long correlationId = CorrelationIdGenerator.generateCorrelationId();
                        log.Info(string.Format(
                                "Consumer continue consuming(mode={0}, topic={1}, partition={2}, groupId={3}, correlationId={4}, sessionId={5}), since lease acquired",
                                Context.ConsumerType, Context.Topic.Name, PartitionId, Context.GroupId, correlationId, Context.SessionId));


                        StartConsuming(key, correlationId);

                        log.Info(string.Format(
                                "Consumer pause consuming(mode={0}, topic={1}, partition={2}, groupId={3}, correlationId={4}, sessionId={5}), since lease expired",
                                Context.ConsumerType, Context.Topic.Name, PartitionId, Context.GroupId, correlationId, Context.SessionId));
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format("Exception occurred in consumer's run method(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
                }
            }

            pullMessageTaskExecutor.Shutdown();
            renewLeaseTaskExecutor.Shutdown();
            log.Info(string.Format("Consumer stopped(mode={0}, topic={1}, partition={2}, groupId={3}, sessionId={4})", 
                    Context.ConsumerType, Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId));
        }

        private void StartConsuming(ConsumerLeaseKey key, long correlationId)
        {
            ConsumerNotifier.Register(correlationId, Context);
            DoBeforeConsuming(key, correlationId);
            msgs.Clear();
            ISchedulePolicy noMessageSchedulePolicy = new ExponentialSchedulePolicy(Config.NoMessageWaitBaseMillis, Config.NoMessageWaitMaxMillis);

            while (!IsClosed() && !leaseRef.ReadFullFence().Expired)
            {

                try
                {
                    // if leaseRemainingTime < stopConsumerTimeMillsBeforLeaseExpired, stop
                    if (leaseRef.ReadFullFence().RemainingTime <= Config.StopConsumerTimeMillsBeforLeaseExpired)
                    {
                        break;
                    }

                    if (msgs.Count == 0)
                    {
                        SchedulePullMessagesTask();
                    }

                    if (msgs.Count != 0)
                    {
                        ConsumeMessages(correlationId);
                        noMessageSchedulePolicy.Succeess();
                    }
                    else
                    {
                        noMessageSchedulePolicy.Fail(true);
                    }

                }
                catch (Exception e)
                {
                    log.Error(string.Format("Exception occurred while consuming message(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
                }
            }

            ConsumerNotifier.Deregister(correlationId);
            leaseRef.WriteFullFence(null);
            DoAfterConsuming(key, correlationId);
        }

        private void RenewLeaseTaskRun(RenewLeaseTask task)
        {
            int delay = (int)task.Delay;
            ConsumerLeaseKey key = task.Key;

            if (IsClosed())
            {
                return;
            }

            ILease lease = leaseRef.ReadFullFence();
            if (lease != null)
            {
                if (lease.RemainingTime > 0)
                {
                    LeaseAcquireResponse response = LeaseManager.TryRenewLease(key, lease);
                    if (response != null && response.Acquired)
                    {
                        lease.ExpireTime = response.Lease.ExpireTime;
                        ScheduleRenewLeaseTask(key,
                            lease.RemainingTime - Config.RenewLeaseTimeMillisBeforeExpired);
                    }
                    else
                    {
                        if (response != null && response.NextTryTime > 0)
                        {
                            ScheduleRenewLeaseTask(key, response.NextTryTime - SystemClockService.Now());
                        }
                        else
                        {
                            ScheduleRenewLeaseTask(key, Config.DefaultLeaseRenewDelayMillis);
                        }
                    }
                }
            }
        }

        protected void ScheduleRenewLeaseTask(ConsumerLeaseKey key, long delay)
        {
            int sKey = scheduleKey.AtomicAddAndGet(1);
            renewLeaseTaskExecutor.Produce(sKey, new RenewLeaseTask(key, delay), (int)delay);
        }

        private void AcquireLease(ConsumerLeaseKey key)
        {
            long nextTryTime = SystemClockService.Now();

            while (!IsClosed())
            {
                try
                {
                    WaitForNextTryTime(nextTryTime);

                    if (IsClosed())
                    {
                        return;
                    }

                    LeaseAcquireResponse response = LeaseManager.TryAcquireLease(key);

                    if (response != null && response.Acquired && !response.Lease.Expired)
                    {
                        leaseRef.WriteFullFence(response.Lease);
                        ScheduleRenewLeaseTask(key,
                            leaseRef.ReadFullFence().RemainingTime - Config.RenewLeaseTimeMillisBeforeExpired);
                        return;
                    }
                    else
                    {
                        if (response != null && response.NextTryTime > 0)
                        {
                            nextTryTime = response.NextTryTime;
                        }
                        else
                        {
                            nextTryTime = SystemClockService.Now() + Config.DefaultLeaseAcquireDelayMillis;
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format("Exception occurred while acquiring lease(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
                }
            }
        }

        private void WaitForNextTryTime(long nextTryTime)
        {
            while (true)
            {
                if (!IsClosed())
                {
                    int timeToNextTry = (int)(nextTryTime - SystemClockService.Now());
                    if (timeToNextTry > 0)
                    {
                        Thread.Sleep(timeToNextTry);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private void ConsumeMessages(long correlationId)
        {
            List<IConsumerMessage> msgsToConsume = new List<IConsumerMessage>();

            msgs.DrainTo(msgsToConsume);

            ConsumerNotifier.MessageReceived(correlationId, msgsToConsume);
        }

        private List<IConsumerMessage> DecodeBatches(List<TppConsumerMessageBatch> batches, Type bodyClazz,
                                                     IoSession channel)
        {
            List<IConsumerMessage> msgs = new List<IConsumerMessage>();
            foreach (TppConsumerMessageBatch batch in batches)
            {
                List<MessageMeta> msgMetas = batch.MessageMetas;
                IoBuffer batchData = batch.Data;

                int partition = batch.Partition;

                for (int j = 0; j < msgMetas.Count; j++)
                {
                    BaseConsumerMessage baseMsg = MessageCodec.Decode(batch.Topic, batchData, bodyClazz);
                    BrokerConsumerMessage brokerMsg = new BrokerConsumerMessage(baseMsg);
                    MessageMeta messageMeta = msgMetas[j];
                    brokerMsg.Partition = partition;
                    brokerMsg.Priority = messageMeta.Priority == 0 ? true : false;
                    brokerMsg.Resend = messageMeta.Resend;
                    brokerMsg.RetryTimesOfRetryPolicy = retryPolicy.GetRetryTimes();
                    brokerMsg.Channel = channel;
                    brokerMsg.MsgSeq = messageMeta.Id;

                    msgs.Add(DecorateBrokerMessage(brokerMsg));
                }
            }

            return msgs;
        }

        protected void SchedulePullMessagesTask()
        {
            if (!IsClosed() && pullTaskRunning.CompareAndSet(false, true))
            {
                pullMessageTaskExecutor.Produce(GetPullMessageTask());
            }
        }

        public abstract class BasePullMessagesTask
        {
            public long CorrelationId { get; set; }

            public ISchedulePolicy NoEndpointSchedulePolicy { get; set; }

            public BaseConsumerTask BaseConsumerTask { get; set; }

            public BasePullMessagesTask(long correlationId, ISchedulePolicy noEndpointSchedulePolicy, BaseConsumerTask baseConsumerTask)
            {
                CorrelationId = correlationId;
                NoEndpointSchedulePolicy = noEndpointSchedulePolicy;
                BaseConsumerTask = baseConsumerTask;
            }

            public abstract PullMessageCommandV2 CreatePullMessageCommand(int timeout);

            public abstract void ResultReceived(PullMessageResultCommandV2 ack);

        }

        private void PullMessagesTaskRun(BasePullMessagesTask task)
        {
            try
            {
                if (IsClosed() || msgs.Count > 0)
                {
                    return;
                }

                Endpoint endpoint = EndpointManager.GetEndpoint(Context.Topic.Name, PartitionId);

                if (endpoint == null)
                {
                    log.Warn(string.Format("No endpoint found for topic {0} partition {1}, will retry later",
                            Context.Topic.Name, PartitionId));
                    task.NoEndpointSchedulePolicy.Fail(true);
                    return;
                }
                else
                {
                    task.NoEndpointSchedulePolicy.Succeess();
                }


                ILease lease = leaseRef.ReadFullFence();
                if (lease != null)
                {
                    int timeout = (int)lease.RemainingTime;

                    if (timeout > 0)
                    {
                        PullMessages(endpoint, timeout, task);
                    }
                }
            }
            catch (Exception e)
            {
                ITransaction t = Cat.NewTransaction("Message.Pull.Internal", Context.Topic.Name);
                t.Status = CatConstants.SUCCESS;
                t.AddData("msg", e.Message);
                t.Complete();
            }
            finally
            {
                pullTaskRunning.WriteFullFence(false);
            }
        }

        protected abstract BasePullMessagesTask GetPullMessageTask();

        protected abstract void DoBeforeConsuming(ConsumerLeaseKey key, long correlationId);

        protected abstract void DoAfterConsuming(ConsumerLeaseKey key, long correlationId);

        protected abstract BrokerConsumerMessage DecorateBrokerMessage(BrokerConsumerMessage brokerMsg);

        private void PullMessages(Endpoint endpoint, int timeout, BasePullMessagesTask task)
        {
            SettableFuture<PullMessageResultCommandV2> future = SettableFuture<PullMessageResultCommandV2>.Create();
            PullMessageCommandV2 cmd = task.CreatePullMessageCommand(timeout);

            cmd.Header.CorrelationId = task.CorrelationId;
            cmd.setFuture(future);

            PullMessageResultCommandV2 ack = null;

            try
            {
                PullMessageResultMonitor.Monitor(cmd);
                EndpointClient.WriteCommand(endpoint, cmd, timeout);

                try
                {
                    ack = future.Get(timeout);
                }
                catch
                {
                }
                finally
                {
                    PullMessageResultMonitor.Remove(cmd);
                }

                if (ack != null)
                {
                    AppendMsgToQueue(ack, task.CorrelationId);
                    task.ResultReceived(ack);
                }
            }
            finally
            {
                if (ack != null)
                {
                    ack.Release();
                }
            }
        }

        private void AppendMsgToQueue(PullMessageResultCommandV2 ack, long correlationId)
        {
            List<TppConsumerMessageBatch> batches = ack.Batches;
            if (batches != null && batches.Count != 0)
            {
                ConsumerContext context = ConsumerNotifier.Find(correlationId);
                if (context != null)
                {
                    Type bodyClazz = context.MessageClazz;

                    List<IConsumerMessage> decodedMsgs = DecodeBatches(batches, bodyClazz, ack.Channel);
                    msgs.AddAll(decodedMsgs);
                }
                else
                {
                    log.Warn(string.Format("Can not find consumerContext(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId,
                            Context.SessionId));
                }
            }
        }


        public void Close()
        {
            closed = true;
        }

        public class RenewLeaseTask
        {
            public ConsumerLeaseKey Key{ get; private set; }

            public long Delay{ get; private set; }

            public RenewLeaseTask(ConsumerLeaseKey key, long delay)
            {
                Key = key;
                Delay = delay;
            }
        }
    }
}

