using System;

namespace Arch.CMessaging.Client.Consumer.Engine.Bootstrap.Strategy
{
    public interface IConsumerTask
    {
        void Start();

        void Close();
    }
}

