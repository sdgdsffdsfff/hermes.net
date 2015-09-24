using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Core.Future;

namespace Arch.CMessaging.Client.Producer.Monitor
{
    public interface ISendMessageResultMonitor
    {
        IFuture<bool> Monitor(SendMessageCommand command);

        void Cancel(SendMessageCommand cmd);

        void ResultReceived(SendMessageResultCommand result);
    }
}
