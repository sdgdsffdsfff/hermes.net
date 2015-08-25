using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Config;
using Arch.CMessaging.Client.Net.Core.Service;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Transport.Command.Processor;
using Freeway.Logging;

namespace Arch.CMessaging.Client.Transport.EndPoint
{
    public class DefaultClientChannelInboundHandler : IoHandlerAdapter
    {
        private CoreConfig config;
        private EndpointSession endpointSession;
        private DefaultEndpointClient endpointClient;
        private CommandProcessorManager cmdProcessorManager;
        private Arch.CMessaging.Client.MetaEntity.Entity.Endpoint endpoint;
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultClientChannelInboundHandler));
        public DefaultClientChannelInboundHandler(
            CommandProcessorManager cmdProcessorManager,
            Arch.CMessaging.Client.MetaEntity.Entity.Endpoint endpoint,
            EndpointSession endpointSession,
            DefaultEndpointClient endpointClient,
            CoreConfig config)
        {
            this.cmdProcessorManager = cmdProcessorManager;
            this.endpoint = endpoint;
            this.endpointSession = endpointSession;
            this.endpointClient = endpointClient;
            this.config = config;
        }

        public override void MessageReceived(IoSession session, object message)
        {
            var command = message as ICommand;
            cmdProcessorManager.Offer(new CommandProcessorContext(command, session));
        }

        public override void SessionIdle(IoSession session, IdleStatus status)
        {
            endpointClient.RemoveSession(endpoint, endpointSession);
        }
    }
}
