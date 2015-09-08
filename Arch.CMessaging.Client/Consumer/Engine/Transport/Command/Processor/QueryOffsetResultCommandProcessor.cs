using System;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Transport.Command;
using System.Collections.Generic;
using Arch.CMessaging.Client.Consumer.Engine.Monitor;
using Arch.CMessaging.Client.Transport.Command.Processor;

namespace Arch.CMessaging.Client.Consumer.Engine.Transport.Command.Processor
{

    [Named(ServiceType = typeof(ICommandProcessor), ServiceName = "QueryOffsetResultCommandProcessor")]
    public class QueryOffsetResultCommandProcessor : ICommandProcessor
    {
        [Inject]
        private IQueryOffsetResultMonitor queryResultMonitor;

        public List<CommandType> CommandTypes()
        {
            return new List<CommandType>{ CommandType.ResultQueryOffset };
        }

        public void Process(CommandProcessorContext ctx)
        {
            QueryOffsetResultCommand cmd = (QueryOffsetResultCommand)ctx.Command;
            queryResultMonitor.ResultReceived(cmd);
        }
    }
}

