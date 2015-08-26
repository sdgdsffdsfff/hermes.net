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

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public class LongPollingConsumerTask
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LongPollingConsumerTask));

        public IConsumerNotifier ConsumerNotifier { get; set; }

        public IMessageCodec MessageCodec { get; set; }

        public IEndpointManager EndpointManager { get; set; }

        public IEndpointClient EndpointClient { get; set; }

        public ILeaseManager<ConsumerLeaseKey> LeaseManager { get; set; }

        public ISystemClockService SystemClockService { get; set; }

        public ConsumerConfig Config{ get; set; }

        private ProducerConsumer<PullMessagesTask> pullMessageTaskExecutorService;

        private TimeoutNotifyProducerConsumer<RenewLeaseTask> renewLeaseTaskExecutorService;

        public IPullMessageResultMonitor PullMessageResultMonitor { get; set; }

        private BlockingQueue<IConsumerMessage> msgs;

        private int cacheSize;

        private ConsumerContext Context;

        private int PartitionId;

        private ThreadSafe.Boolean pullTaskRunning = new ThreadSafe.Boolean(false);

        private ThreadSafe.AtomicReference<ILease> leaseRef = new ThreadSafe.AtomicReference<ILease>(null);

        private volatile bool closed = false;

        private IRetryPolicy retryPolicy;

        private ThreadSafe.Integer scheduleKey = new ThreadSafe.Integer(0);

        public LongPollingConsumerTask(ConsumerContext context, int partitionId, int cacheSize,
                                       IRetryPolicy retryPolicy)
        {
            Context = context;
            PartitionId = partitionId;
            this.cacheSize = cacheSize;
            msgs = new BlockingQueue<IConsumerMessage>(cacheSize);
            this.retryPolicy = retryPolicy;

            pullMessageTaskExecutorService = new ProducerConsumer<PullMessagesTask>(int.MaxValue);
            pullMessageTaskExecutorService.OnConsume += RunPullMessageTask;

            renewLeaseTaskExecutorService = new TimeoutNotifyProducerConsumer<RenewLeaseTask>(int.MaxValue);
            renewLeaseTaskExecutorService.OnConsume += RunRenewLeaseTask;
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
            PullMessagesTask task = (e.ConsumingItem as SingleConsumingItem<PullMessagesTask>).Item;
            PullMessagesTaskRun(task.CorrelationId);
        }

        private bool IsClosed()
        {
            return closed;
        }

        public void Run()
        {
            log.Info(string.Format("Consumer started(topic={0}, partition={1}, groupId={2}, sessionId={3})", Context.Topic.Name,
                    PartitionId, Context.GroupId, Context.SessionId));
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
                                "Consumer continue consuming(topic={0}, partition={1}, groupId={2}, correlationId={3}, sessionId={4}), since lease acquired",
                                Context.Topic.Name, PartitionId, Context.GroupId, correlationId,
                                Context.SessionId));

                        StartConsumingMessages(key, correlationId);

                        log.Info(string.Format(
                                "Consumer pause consuming(topic={0}, partition={1}, groupId={2}, correlationId={3}, sessionId={4}), since lease expired",
                                Context.Topic.Name, PartitionId, Context.GroupId, correlationId,
                                Context.SessionId));
                    }
                }
                catch (Exception e)
                {
                    log.Error(string.Format("Exception occurred in consumer's run method(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
                }
            }

            pullMessageTaskExecutorService.Shutdown();
            renewLeaseTaskExecutorService.Shutdown();
            log.Info(string.Format("Consumer stopped(topic={0}, partition={1}, groupId={2}, sessionId={3})", Context.Topic.Name,
                    PartitionId, Context.GroupId, Context.SessionId));
        }

        private void StartConsumingMessages(ConsumerLeaseKey key, long correlationId)
        {
            ConsumerNotifier.Register(correlationId, Context);

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
                        SchedulePullMessagesTask(correlationId);
                    }

                    if (msgs.Count != 0)
                    {
                        ConsumeMessages(correlationId);
                    }
                    else
                    {
                        Thread.Sleep(Config.NoMessageWaitIntervalMillis);
                    }

                }
                catch (Exception e)
                {
                    log.Error(string.Format("Exception occurred while consuming message(topic={0}, partition={1}, groupId={2}, sessionId={3})",
                            Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
                }
            }

            // consume all remaining messages
            if (msgs.Count != 0)
            {
                ConsumeMessages(correlationId);
            }

            ConsumerNotifier.Deregister(correlationId);
            leaseRef.WriteFullFence(null);
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

        private void ScheduleRenewLeaseTask(ConsumerLeaseKey key, long delay)
        {
            int sKey = scheduleKey.AtomicAddAndGet(1);
            renewLeaseTaskExecutorService.Produce(sKey, new RenewLeaseTask(key, delay), (int)delay);
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

                    msgs.Add(brokerMsg);
                }
            }

            return msgs;
        }

        private void SchedulePullMessagesTask(long correlationId)
        {
            if (!IsClosed() && pullTaskRunning.CompareAndSet(false, true))
            {
                pullMessageTaskExecutorService.Produce(new PullMessagesTask(correlationId));
            }
        }

        private void PullMessagesTaskRun(long correlationId)
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
                    Thread.Sleep(Config.NoEndpointWaitIntervalMillis);
                    return;
                }


                ILease lease = leaseRef.ReadFullFence();
                if (lease != null)
                {
                    int timeout = (int)lease.RemainingTime;

                    if (timeout > 0)
                    {
                        PullMessages(endpoint, timeout, correlationId);
                    }
                }
            }
            catch (Exception e)
            {
                log.Warn(string.Format("Exception occurred while pulling message(topic={0}, partition={1}, groupId={2}, sessionId={3}).",
                        Context.Topic.Name, PartitionId, Context.GroupId, Context.SessionId), e);
            }
            finally
            {
                pullTaskRunning.WriteFullFence(false);
            }
        }

        private void PullMessages(Endpoint endpoint, int timeout, long correlationId)
        {
            SettableFuture<PullMessageResultCommand> future = SettableFuture<PullMessageResultCommand>.Create();
            PullMessageCommand cmd = new PullMessageCommand(Context.Topic.Name, PartitionId,
                                         Context.GroupId, cacheSize - msgs.Count, SystemClockService.Now() + timeout
                                         - 500L);

            cmd.Header.CorrelationId = correlationId;
            cmd.setFuture(future);

            PullMessageResultCommand ack = null;

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
                    PullMessageResultMonitor.Remove(cmd);
                }

                if (ack != null)
                {
                    AppendMsgToQueue(ack, correlationId);
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

        private void AppendMsgToQueue(PullMessageResultCommand ack, long correlationId)
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

        class PullMessagesTask
        {
            public long CorrelationId { get; set; }

            public PullMessagesTask(long correlationId)
            {
                CorrelationId = correlationId;
            }

        }

        class RenewLeaseTask
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

