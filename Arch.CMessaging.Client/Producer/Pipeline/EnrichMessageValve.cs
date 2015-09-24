using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Pipeline.spi;
using Arch.CMessaging.Client.Core.Pipeline;
using Arch.CMessaging.Client.Core.Ioc;
using Freeway.Logging;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Env;
using Arch.CMessaging.Client.Producer.Config;

namespace Arch.CMessaging.Client.Producer.Pipeline
{
    [Named(ServiceType = typeof(IValve), ServiceName = EnrichMessageValve.ID)]
    public class EnrichMessageValve : IValve, IInitializable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EnrichMessageValve));
        public const string ID = "enrich";

        [Inject]
        private ProducerConfig config;

        private bool logEnrichInfo = false;

        public void Handle(IPipelineContext ctx, Object payload)
        {
            var msg = (ProducerMessage)payload;
            var topic = msg.Topic;
            if (string.IsNullOrEmpty(topic))
            {
                log.Error("Topic not set, won't send");
                return;
            }

            enrichPartitionKey(msg, Local.IPV4);
            enrichRefKey(msg);
            enrichMessageProperties(msg, Local.IPV4);

            ctx.Next(payload);
        }

        private void enrichRefKey(ProducerMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Key))
            {
                String refKey = System.Guid.NewGuid().ToString();
                if (logEnrichInfo)
                {
                    log.Info(string.Format("Ref key not set, will set uuid as ref key(topic={0}, ref key={1})", msg.Topic, refKey));
                }
                msg.Key = refKey;
            }
        }

        private void enrichMessageProperties(ProducerMessage msg, String ip)
        {
            msg.AddDurableSysProperty(MessagePropertyNames.PRODUCER_IP, ip);
        }

        private void enrichPartitionKey(ProducerMessage msg, String ip)
        {
            if (string.IsNullOrEmpty(msg.PartitionKey))
            {
                if (logEnrichInfo)
                {
                    log.Info(string.Format("Parition key not set, will set ip as partition key(topic={0}, ip={1})", msg.Topic, ip));
                }
                msg.PartitionKey = ip;
            }
        }

        public void Initialize()
        {
            logEnrichInfo = config.LogEnrichInfoEnabled;
        }
    }

}

