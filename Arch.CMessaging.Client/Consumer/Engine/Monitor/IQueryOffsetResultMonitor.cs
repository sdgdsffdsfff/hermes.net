using System;
using Arch.CMessaging.Client.Transport.Command;

namespace Arch.CMessaging.Client.Consumer.Engine.Monitor
{
    public interface IQueryOffsetResultMonitor
    {
        void Monitor(QueryOffsetCommand cmd);

        void ResultReceived(QueryOffsetResultCommand ack);

        void Remove(QueryOffsetCommand cmd);
    }
}

