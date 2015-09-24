using System;
using Arch.CMessaging.Client.Core.Ioc;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Consumer.Engine
{
    public abstract class IEngine
    {
        public abstract ISubscribeHandle Start(Subscriber subscribers);

        public static IEngine GetInstance()
        {
            return ComponentLocator.Lookup<IEngine>();
        }
    }
}

