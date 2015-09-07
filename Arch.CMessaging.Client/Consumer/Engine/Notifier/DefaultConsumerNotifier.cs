using System;
using System.Collections.Generic;
using Freeway.Logging;
using Arch.CMessaging.Client.Core.Pipeline;
using Arch.CMessaging.Client.Consumer.Engine.Config;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Core.Env;
using Arch.CMessaging.Client.Consumer.Engine;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Consumer.Build;
using Arch.CMessaging.Client.Consumer.Api;
using Arch.CMessaging.Client.MetaEntity.Entity;

namespace Arch.CMessaging.Client.Consumer.Engine.Notifier
{
    [Named(ServiceType = typeof(IConsumerNotifier))]
    public class DefaultConsumerNotifier : IConsumerNotifier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultConsumerNotifier));

        private ConcurrentDictionary<long, Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>>> consumerContexs = new ConcurrentDictionary<long, Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>>>();

        [Inject(BuildConstants.CONSUMER)]
        private IPipeline<object> pipeline;

        [Inject]
        private ConsumerConfig config;

        [Inject]
        private ISystemClockService systemClockService;

        [Inject]
        private IClientEnvironment clientEnv;

        public void Register(long correlationId, ConsumerContext context)
        {
            try
            {
                int threadCount = Convert.ToInt32(clientEnv.GetConsumerConfig(context.Topic.Name).GetProperty(
                                          "consumer.notifier.threadcount", config.DefaultNotifierThreadCount));

                BlockingQueue<Action> blockingQueue = new TransferBlockingQueue<Action>(threadCount);
                ProducerConsumer<Action> threadPool = new ProducerConsumer<Action>(blockingQueue, threadCount);

                threadPool.OnConsume += DispatchMessages;


                MessageListenerConfig messageListenerConfig = context.MessageListenerConfig;
                INotifyStrategy notifyStrategy = null;
                if (!Storage.KAFKA.Equals(context.Topic.StorageType)//
                    && messageListenerConfig.StrictlyOrdering)
                {
                    notifyStrategy = new StrictlyOrderedNotifyStrategy(messageListenerConfig.GetStrictlyOrderingRetryPolicy());
                }
                else
                {
                    notifyStrategy = new DefaultNotifyStrategy();
                }

                consumerContexs.TryAdd(correlationId, new Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>>(
                        context, notifyStrategy, threadPool));
            }
            catch (Exception e)
            {
                throw new Exception("Register consumer notifier failed", e);
            }
        }

        public void Deregister(long correlationId)
        {
            Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>> triple = null;
            consumerContexs.TryRemove(correlationId, out triple);
            ConsumerContext context = triple.First;
            triple.Last.Shutdown();
            return;
        }

        public void DispatchMessages(object sender, ConsumeEventArgs args)
        {
            SingleConsumingItem<Action> item = (SingleConsumingItem<Action>)args.ConsumingItem;
            item.Item.Invoke();
        }

        public void MessageReceived(long correlationId, List<IConsumerMessage> msgs)
        {
            Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>> triple = consumerContexs[correlationId];
            ConsumerContext context = triple.First;
            INotifyStrategy notifyStrategy = triple.Middle;
            ProducerConsumer<Action> executorService = triple.Last;

            executorService.Produce(delegate
                {
                    try
                    {
                        foreach (IConsumerMessage msg in msgs)
                        {
                            if (msg is BrokerConsumerMessage)
                            {
                                BrokerConsumerMessage bmsg = (BrokerConsumerMessage)msg;
                                bmsg.CorrelationId = correlationId;
                                bmsg.GroupId = context.GroupId;
                            }
                        }

                        notifyStrategy.Notify(msgs, context, executorService, pipeline);
                    }
                    catch (Exception e)
                    {
                        log.Error(
                            string.Format("Exception occurred while calling messageReceived(correlationId={0}, topic={1}, groupId={2}, sessionId={3})",
                                correlationId, context.Topic.Name, context.GroupId, context.SessionId), e);
                    }
                });
        }

        public ConsumerContext Find(long correlationId)
        {
            Triple<ConsumerContext, INotifyStrategy, ProducerConsumer<Action>> triple = null;
            consumerContexs.TryGetValue(correlationId, out triple);
            return triple == null ? null : triple.First;
        }
    }

    public class TransferBlockingQueue<TItem> : BlockingQueue<TItem>
    {
        public TransferBlockingQueue(int capacity)
            : base(capacity)
        {
        }

        public override bool Offer(TItem item)
        {
            // -1 means not timeout
            return Put(item, -1);
        }
    }
}

