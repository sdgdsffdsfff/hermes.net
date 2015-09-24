using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Future;

namespace Arch.CMessaging.Client.Producer.Monitor
{
    public interface ISendMessageAcceptanceMonitor
    {
        IFuture<bool> Monitor(long correlationID);

        void Cancel(long correlationId);

        void Recieved(long correlationID, bool success);
    }
}
