using System;
using Arch.CMessaging.Client.Transport.Command.Processor;
using System.Collections.Generic;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Consumer.Engine.Transport.Command.Processor
{
    [Named(ServiceType = typeof(ICommandProcessor), ServiceName = "PullMessageResultCommandProcessor")]
    public class PullMessageResultCommandProcessor : ICommandProcessor
    {
        [Inject]
        private IPullMessageResultMonitor messageResultMonitor;

        public List<CommandType> CommandTypes()
        {
            return new List<CommandType>{ CommandType.ResultMessagePull };
        }

        public void Process(CommandProcessorContext ctx)
        {
            PullMessageResultCommandV2 cmd = (PullMessageResultCommandV2)ctx.Command;
            cmd.Channel = ctx.Session;
            messageResultMonitor.ResultReceived(cmd);
        }
    }
}

