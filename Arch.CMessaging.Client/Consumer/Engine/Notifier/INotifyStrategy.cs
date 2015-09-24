using System;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Pipeline;

namespace Arch.CMessaging.Client.Consumer.Engine.Notifier
{
    public interface INotifyStrategy
    {
        void Notify(List<IConsumerMessage> msgs, ConsumerContext context, ProducerConsumer<Action> executorService, IPipeline<object> pipeline);
    }
}

