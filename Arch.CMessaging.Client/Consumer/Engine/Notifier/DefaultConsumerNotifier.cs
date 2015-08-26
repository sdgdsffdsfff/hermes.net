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

namespace Arch.CMessaging.Client.Consumer.Engine.Notifier
{
    [Named(ServiceType = typeof(IConsumerNotifier))]
    public class DefaultConsumerNotifier : IConsumerNotifier
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultConsumerNotifier));

        private ConcurrentDictionary<long, Pair<ConsumerContext, ProducerConsumer<Action>>> consumerContexs = new ConcurrentDictionary<long, Pair<ConsumerContext, ProducerConsumer<Action>>>();

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

                var pair = new Pair<ConsumerContext, ProducerConsumer<Action>>(context, threadPool);
                consumerContexs.TryAdd(correlationId, pair);
            }
            catch (Exception e)
            {
                throw new Exception("Register consumer notifier failed", e);
            }
        }

        public void Deregister(long correlationId)
        {
            Pair<ConsumerContext, ProducerConsumer<Action>> pair = null;
            consumerContexs.TryRemove(correlationId, out pair);
            ConsumerContext context = pair.Key;
            pair.Value.Shutdown();
            return;
        }

        public void DispatchMessages(object sender, ConsumeEventArgs args)
        {
            SingleConsumingItem<Action> item = (SingleConsumingItem<Action>)args.ConsumingItem;
            item.Item.Invoke();
        }

        public void MessageReceived(long correlationId, List<IConsumerMessage> msgs)
        {
            Pair<ConsumerContext, ProducerConsumer<Action>> pair = consumerContexs[correlationId];
            ConsumerContext context = pair.Key;
            ProducerConsumer<Action> executorService = pair.Value;

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

                        pipeline.Put(new Pair<ConsumerContext, List<IConsumerMessage>>(context, msgs));
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
            Pair<ConsumerContext, ProducerConsumer<Action>> pair = null;
            consumerContexs.TryGetValue(correlationId, out pair);
            return pair == null ? null : pair.Key;
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

