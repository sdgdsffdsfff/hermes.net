using System;
using Arch.CMessaging.Client.Transport.Command;

namespace Arch.CMessaging.Client.Consumer.Engine.Monitor
{
    public interface IPullMessageResultMonitor
    {
        void Monitor(PullMessageCommandV2 cmd);

        void ResultReceived(PullMessageResultCommandV2 ack);

        void Remove(PullMessageCommandV2 cmd);
    }
}

