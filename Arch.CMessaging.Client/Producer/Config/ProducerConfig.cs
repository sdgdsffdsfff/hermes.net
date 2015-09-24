using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Env;

namespace Arch.CMessaging.Client.Producer.Config
{
    [Named(ServiceType = typeof(ProducerConfig))]
    public class ProducerConfig : IInitializable
    {
        public const int DEFAULT_BROKER_SENDER_NETWORK_IO_THREAD_COUNT = 10;

        public const long DEFAULT_BROKER_SENDER_SEND_TIMEOUT = 10 * 1000L;

        public const long DEFAULT_BROKER_SENDER_READ_TIMEOUT = 10 * 1000L;

        public const int DEFAULT_BROKER_SENDER_TASK_QUEUE_SIZE = 500000;

        public const int DEFAULT_BROKER_SENDER_BATCH_SIZE = 500;

        public const int DEFAULT_BROKER_SENDER_NETWORK_IO_CHECK_INTERVAL_BASE_MILLIS = 5;

        public const int DEFAULT_BROKER_SENDER_NETWORK_IO_CHECK_INTERVAL_MAX_MILLIS = 50;

        public const int DEFAULT_PRODUCER_CALLBACK_THREAD_COUNT = 50;

        [Inject]
        private IClientEnvironment clientEnv;

        public void Initialize()
        {
            BrokerSenderNetworkIoThreadCount = DEFAULT_BROKER_SENDER_NETWORK_IO_THREAD_COUNT;
            String brokerSenderNetworkIoThreadCountStr = clientEnv.GetGlobalConfig().GetProperty("producer.networkio.threadcount");
            if (!String.IsNullOrWhiteSpace(brokerSenderNetworkIoThreadCountStr))
            {
                BrokerSenderNetworkIoThreadCount = Convert.ToInt32(brokerSenderNetworkIoThreadCountStr);
            }

            BrokerSenderSendTimeoutMillis = DEFAULT_BROKER_SENDER_SEND_TIMEOUT;

            SendMessageReadResultTimeoutMillis = DEFAULT_BROKER_SENDER_READ_TIMEOUT;

            BrokerSenderTaskQueueSize = DEFAULT_BROKER_SENDER_TASK_QUEUE_SIZE;
            String brokerSenderTaskQueueSizeStr = clientEnv.GetGlobalConfig().GetProperty("producer.sender.taskqueue.size");
            if (!String.IsNullOrWhiteSpace(brokerSenderTaskQueueSizeStr))
            {
                BrokerSenderTaskQueueSize = Convert.ToInt32(brokerSenderTaskQueueSizeStr);
            }

            BrokerSenderNetworkIoCheckIntervalBaseMillis = DEFAULT_BROKER_SENDER_NETWORK_IO_CHECK_INTERVAL_BASE_MILLIS;
            String brokerSenderNetworkIoCheckIntervalBaseMillisStr = clientEnv.GetGlobalConfig().GetProperty("producer.networkio.interval.base");
            if (!String.IsNullOrWhiteSpace(brokerSenderNetworkIoCheckIntervalBaseMillisStr))
            {
                BrokerSenderNetworkIoCheckIntervalBaseMillis = Convert.ToInt32(brokerSenderNetworkIoCheckIntervalBaseMillisStr);
            }

            BrokerSenderNetworkIoCheckIntervalMaxMillis = DEFAULT_BROKER_SENDER_NETWORK_IO_CHECK_INTERVAL_MAX_MILLIS;
            String brokerSenderNetworkIoCheckIntervalMaxMillisStr = clientEnv.GetGlobalConfig().GetProperty("producer.networkio.interval.max");
            if (!String.IsNullOrWhiteSpace(brokerSenderNetworkIoCheckIntervalMaxMillisStr))
            {
                BrokerSenderNetworkIoCheckIntervalMaxMillis = Convert.ToInt32(brokerSenderNetworkIoCheckIntervalMaxMillisStr);
            }

            ProducerCallbackThreadCount = DEFAULT_PRODUCER_CALLBACK_THREAD_COUNT;
            String producerCallbackThreadCountStr = clientEnv.GetGlobalConfig().GetProperty("producer.callback.threadcount");
            if (!String.IsNullOrWhiteSpace(producerCallbackThreadCountStr))
            {
                ProducerCallbackThreadCount = Convert.ToInt32(producerCallbackThreadCountStr);
            }

            BrokerSenderBatchSize = DEFAULT_BROKER_SENDER_BATCH_SIZE;
            String brokerSenderBatchSizeStr = clientEnv.GetGlobalConfig().GetProperty("producer.sender.batchsize");
            if (!String.IsNullOrWhiteSpace(brokerSenderBatchSizeStr))
            {
                BrokerSenderBatchSize = Convert.ToInt32(brokerSenderBatchSizeStr);
            }

            LogEnrichInfoEnabled = false;
            String logEnrichInfoEnabledStr = clientEnv.GetGlobalConfig().GetProperty("logEnrichInfo", "false");
            if (string.Equals("true", logEnrichInfoEnabledStr, StringComparison.OrdinalIgnoreCase))
            {
                LogEnrichInfoEnabled = true;
            }

        }

        public int BrokerSenderNetworkIoThreadCount { get; private set; }

        public int BrokerSenderNetworkIoCheckIntervalBaseMillis { get; private set; }

        public int BrokerSenderNetworkIoCheckIntervalMaxMillis { get; private set; }

        public int BrokerSenderBatchSize { get; private set; }

        public long BrokerSenderSendTimeoutMillis { get; private set; }

        public int BrokerSenderTaskQueueSize { get; private set; }

        public int ProducerCallbackThreadCount { get; private set; }

        public long SendMessageReadResultTimeoutMillis { get; private set; }

        public bool LogEnrichInfoEnabled { get; private set; }
    }
}
