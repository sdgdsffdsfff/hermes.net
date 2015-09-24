using System;
using Arch.CMessaging.Client.Core.Ioc;
using Freeway.Logging;
using Arch.CMessaging.Client.Core.MetaService;
using System.Collections.Generic;
using Arch.CMessaging.Client.Consumer.Engine.Bootstrap;
using Arch.CMessaging.Client.MetaEntity.Entity;

namespace Arch.CMessaging.Client.Consumer.Engine
{
    [Named(ServiceType = typeof(IEngine))]
    public class DefaultEngine : IEngine
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultEngine));

        [Inject]
        private IConsumerBootstrapManager consumerManager;

        [Inject]
        private IMetaService metaService;

        public override ISubscribeHandle Start(Subscriber subscriber)
        {
            CompositeSubscribeHandle handle = new CompositeSubscribeHandle();

            List<Topic> topics = metaService.ListTopicsByPattern(subscriber.TopicPattern);

            if (topics == null || topics.Count == 0)
            {
                throw new Exception(string.Format("Can not find any topics matching pattern {0}", subscriber.TopicPattern));
            }

            log.Info(string.Format("Found topics({0}) matching pattern({1}), groupId={2}.",
                    string.Join(",", topics.ConvertAll(t => t.Name)), subscriber.TopicPattern, subscriber.GroupId));

            Validate(topics, subscriber.GroupId);

            foreach (Topic topic in topics)
            {
                ConsumerContext context = new ConsumerContext(topic, subscriber.GroupId, subscriber.Consumer,
                                              subscriber.Consumer.MessageType(), subscriber.ConsumerType, subscriber.MessageListenerConfig);

                try
                {
                    IConsumerBootstrap consumerBootstrap = consumerManager.FindConsumerBootStrap(topic);
                    handle.AddSubscribeHandle(consumerBootstrap.Start(context));

                }
                catch (Exception e)
                {
                    log.Error(string.Format("Failed to start consumer for topic {0}(consumer: groupId={1}, sessionId={2})",
                            topic.Name, context.GroupId, context.SessionId), e);
                    throw e;
                }
            }

            return handle;
        }

        private void Validate(List<Topic> topics, string groupId)
        {
            List<string> failedTopics = new List<string>();
            bool hasError = false;

            foreach (Topic topic in topics)
            {
                if (Endpoint.BROKER.Equals(topic.EndpointType))
                {
                    if (!metaService.ContainsConsumerGroup(topic.Name, groupId))
                    {
                        failedTopics.Add(topic.Name);
                        hasError = true;
                    }
                }
            }

            if (hasError)
            {
                throw new Exception(string.Format("Consumer group {0} not found for topics ({1}), please add consumer group in Hermes-Portal first.", groupId, failedTopics));
            }

        }

    }
}

