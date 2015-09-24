using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Producer
{
    interface IServer
    {
        IClient Client { get; }
    }
}
