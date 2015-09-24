using System;
using Arch.CMessaging.Client.Core.Ioc;
using Freeway.Logging;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Transport.Command;

namespace Arch.CMessaging.Client.Consumer.Engine.Monitor
{

    [Named(ServiceType = typeof(IQueryOffsetResultMonitor))]
    public class DefaultQueryOffsetResultMonitor : IQueryOffsetResultMonitor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultQueryOffsetResultMonitor));

        private ConcurrentDictionary<long, QueryOffsetCommand> cmds = new ConcurrentDictionary<long, QueryOffsetCommand>();

        public void Monitor(QueryOffsetCommand cmd)
        {
            if (cmd != null)
            {
                cmds[cmd.Header.CorrelationId] = cmd;
            }
        }

        public void ResultReceived(QueryOffsetResultCommand result)
        {
            if (result != null)
            {
                QueryOffsetCommand queryOffsetCommand = null;
                cmds.TryRemove(result.Header.CorrelationId, out queryOffsetCommand);

                if (queryOffsetCommand != null)
                {
                    try
                    {
                        queryOffsetCommand.OnResultReceived(result);
                    }
                    catch (Exception e)
                    {
                        log.Warn("Exception occurred while calling resultReceived", e);
                    }
                }
            }
        }

        public void Remove(QueryOffsetCommand cmd)
        {
            QueryOffsetCommand removedCmd = null;
            if (cmd != null)
            {
                cmds.TryRemove(cmd.Header.CorrelationId, out removedCmd);
            }
        }

    }
}

