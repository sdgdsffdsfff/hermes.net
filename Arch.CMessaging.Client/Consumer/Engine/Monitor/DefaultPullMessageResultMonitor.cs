using System;
using Freeway.Logging;
using Arch.CMessaging.Client.Core.Service;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Transport.Command;
using System.Threading;
using System.Collections.Generic;

namespace Arch.CMessaging.Client.Consumer.Engine.Monitor
{
    [Named(ServiceType = typeof(IPullMessageResultMonitor))]
    public class DefaultPullMessageResultMonitor : IPullMessageResultMonitor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultPullMessageResultMonitor));

        private ConcurrentDictionary<long, PullMessageCommandV2> cmds = new ConcurrentDictionary<long, PullMessageCommandV2>();

        public void Monitor(PullMessageCommandV2 cmd)
        {
            if (cmd != null)
            {
                cmds[cmd.Header.CorrelationId] = cmd;
            }
        }

        public void ResultReceived(PullMessageResultCommandV2 result)
        {
            if (result != null)
            {
                PullMessageCommandV2 pullMessageCommand = null;
                cmds.TryRemove(result.Header.CorrelationId, out pullMessageCommand);

                if (pullMessageCommand != null)
                {
                    try
                    {
                        pullMessageCommand.OnResultReceived(result);
                    }
                    catch (Exception e)
                    {
                        log.Warn("Exception occurred while calling resultReceived", e);
                    }
                }
                else
                {
                    result.Release();
                }
            }
        }

        public void Remove(PullMessageCommandV2 cmd)
        {
            PullMessageCommandV2 removedCmd = null;
            if (cmd != null)
            {
                cmds.TryRemove(cmd.Header.CorrelationId, out removedCmd);
            }
        }

    }
}

