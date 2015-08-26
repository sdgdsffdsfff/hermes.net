using System;
using Arch.CMessaging.Client.Core.Lease;
using Arch.CMessaging.Client.Consumer.Engine.Lease;
using Arch.CMessaging.Client.Consumer.Engine.Notifier;
using Arch.CMessaging.Client.Transport.EndPoint;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Consumer.Engine.Config;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Core.Env;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.MetaService;
using Arch.CMessaging.Client.Core.Message.Retry;
using System.Threading;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    [Named(ServiceType = typeof(IBrokerConsumptionStrategy))]
    public class BrokerLongPollingConsumptionStrategy : IBrokerConsumptionStrategy
    {
        [Inject]
        private ILeaseManager<ConsumerLeaseKey> LeaseManager;

        [Inject]
        private IConsumerNotifier ConsumerNotifier;

        [Inject]
        private IEndpointManager EndpointManager;

        [Inject]
        private IEndpointClient EndpointClient;

        [Inject]
        private IMessageCodec MessageCodec;

        [Inject]
        private ConsumerConfig Config;

        [Inject]
        private ISystemClockService SystemClockService;

        [Inject]
        private IPullMessageResultMonitor pullMessageResultMonitor;

        [Inject]
        private IClientEnvironment ClientEnv;

        [Inject]
        private IMetaService metaService;

        public ISubscribeHandle Start(ConsumerContext context, int partitionId)
        {
            try
            {
                int localCacheSize = Convert.ToInt32(ClientEnv.GetConsumerConfig(context.Topic.Name).GetProperty(
                                             "consumer.localcache.size", Config.DefautlLocalCacheSize));

                IRetryPolicy retryPolicy = metaService.FindRetryPolicyByTopicAndGroup(context.Topic.Name, context.GroupId);
                LongPollingConsumerTask consumerTask = new LongPollingConsumerTask(//
                                                           context, //
                                                           partitionId,//
                                                           localCacheSize, //
                                                           retryPolicy);

                consumerTask.EndpointClient = EndpointClient;
                consumerTask.ConsumerNotifier = ConsumerNotifier;
                consumerTask.EndpointManager = EndpointManager;
                consumerTask.LeaseManager = LeaseManager;
                consumerTask.MessageCodec = MessageCodec;
                consumerTask.SystemClockService = SystemClockService;
                consumerTask.Config = Config;
                consumerTask.PullMessageResultMonitor = pullMessageResultMonitor;

                Thread pollingThread = new Thread(consumerTask.Run);
                pollingThread.IsBackground = false;
                pollingThread.Start();
                
                return new BrokerLongPollingSubscribeHandler(consumerTask);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Start Consumer failed(topic={0}, partition={1}, groupId={2})", context
					.Topic.Name, partitionId, context.GroupId), e);
            }
        }

        private class BrokerLongPollingSubscribeHandler : ISubscribeHandle
        {

            private LongPollingConsumerTask ConsumerTask;

            public BrokerLongPollingSubscribeHandler(LongPollingConsumerTask consumerTask)
            {
                ConsumerTask = consumerTask;
            }

            public void Close()
            {
                ConsumerTask.Close();
            }

        }
    }
}

