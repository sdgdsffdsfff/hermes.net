using System;
using Arch.CMessaging.Client.MetaEntity.Entity;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap
{
    public interface IConsumerBootstrapManager
    {
        IConsumerBootstrap FindConsumerBootStrap(Topic topic);
    }
}

