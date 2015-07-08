﻿using System;
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
        private IConsumerBootstrapManager m_consumerManager;

        [Inject]
        private IMetaService m_metaService;

        public override ISubscribeHandle Start(List<Subscriber> subscribers)
        {
            CompositeSubscribeHandle handle = new CompositeSubscribeHandle();

            foreach (Subscriber s in subscribers)
            {
                List<Topic> topics = m_metaService.ListTopicsByPattern(s.TopicPattern);

                if (topics != null && topics.Count != 0)
                {
                    log.Info(string.Format("Found topics({0}) matching pattern({1}), groupId={2}.",
                            string.Join(",", topics.ConvertAll(t => t.Name)), s.TopicPattern, s.GroupId));

                    foreach (Topic topic in topics)
                    {
                        ConsumerContext context = new ConsumerContext(topic, s.GroupId, s.Consumer, s.Consumer.MessageType(), s.ConsumerType);

                        if (validate(topic, context))
                        {
                            try
                            {
                                String endpointType = m_metaService.FindEndpointTypeByTopic(topic.Name);
                                IConsumerBootstrap consumerBootstrap = m_consumerManager.findConsumerBootStrap(endpointType);
                                handle.AddSubscribeHandle(consumerBootstrap.start(context));

                            }
                            catch (Exception e)
                            {
                                log.Error(string.Format("Failed to start consumer for topic {0}(consumer: groupId={1}, sessionId={2})",
                                        topic.Name, context.GroupId, context.SessionId), e);
                            }
                        }
                    }
                }
                else
                {
                    log.Error(string.Format("Can not find any topics matching pattern {0}", s.TopicPattern));
                }
            }

            return handle;
        }

        private bool validate(Topic topic, ConsumerContext context)
        {
            if (Endpoint.BROKER.Equals(topic.EndpointType))
            {
                if (!m_metaService.ContainsConsumerGroup(topic.Name, context.GroupId))
                {
                    log.Error(string.Format("Consumer group {0} not found for topic {1}, please add consumer group in Hermes-Portal first.",
                            context.GroupId, topic.Name));
                    return false;
                }
            }

            return true;
        }
    }
}

