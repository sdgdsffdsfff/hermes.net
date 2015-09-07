using System;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Consumer.Engine.Config;
using Arch.CMessaging.Client.Core.Message.Retry;
using System.Threading;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public abstract class BaseCosumingStrategy : IConsumingStrategy
    {

        [Inject]
        protected ConsumerConfig config;

        public ISubscribeHandle Start(ConsumerContext context, int partitionId)
        {
            try
            {
                int localCacheSize = config.GetLocalCacheSize(context.Topic.Name);

                IConsumerTask consumerTask = GetConsumerTask(context, partitionId, localCacheSize);

                Thread pollingThread = new Thread(consumerTask.Start);
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

        protected abstract IConsumerTask GetConsumerTask(ConsumerContext context, int partitionId, int localCacheSize);


        public class BrokerLongPollingSubscribeHandler : ISubscribeHandle
        {

            private IConsumerTask ConsumerTask;

            public BrokerLongPollingSubscribeHandler(IConsumerTask consumerTask)
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

