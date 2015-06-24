﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Message.Partition;
using Arch.CMessaging.Client.Core.Meta;
using Arch.CMessaging.Client.Producer.Monitor;
using Arch.CMessaging.Client.Transport.EndPoint;
using Arch.CMessaging.Client.Core.Result;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Message;

namespace Arch.CMessaging.Client.Producer.Sender
{
    public abstract class AbstractMessageSender : IMessageSender
    {
        //ioc inject
        private IEndpointManager endpointManager;

        //ioc inject
        private IEndpointClient endpointClient;

        //ioc inject
        private IPartitioningStrategy partitioningAlgo;

        //ioc inject
        private IMetaService metaService;

        //ioc inject
        private ISendMessageAcceptanceMonitor messageAcceptanceMonitor;

        //ioc inject
        private ISendMessageResultMonitor messageResultMonitor;

        public IEndpointManager EndpointManager { get { return endpointManager; } }
        public IEndpointClient EndpointClient { get { return endpointClient; } }
        public IMetaService MetaService { get { return metaService; } }
        public ISendMessageAcceptanceMonitor SendMessageAcceptanceMonitor { get { return messageAcceptanceMonitor; } }
        public ISendMessageResultMonitor SendMessageResultMonitor { get { return messageResultMonitor; } }

        #region IMessageSender Members

        public IFuture<SendResult> Send(ProducerMessage message)
        {
            PreSend(message);
            return DoSend(message);
        }

        #endregion

        protected abstract IFuture<SendResult> DoSend(ProducerMessage message);
        protected void PreSend(ProducerMessage message) 
        {
            var partitionNo = partitioningAlgo.ComputePartitionNo(
                message.PartitionKey, 
                metaService.ListPartitionsByTopic(message.Topic).Count);
            message.Partition = partitionNo;
	    }
    }
}
