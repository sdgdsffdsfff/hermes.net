using System;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Pipeline;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Consumer.Engine.Notifier
{
    public class DefaultNotifyStrategy : INotifyStrategy
    {
        public void Notify(List<IConsumerMessage> msgs, ConsumerContext context, ProducerConsumer<Action> executorService, IPipeline<object> pipeline)
        {
            pipeline.Put(new Pair<ConsumerContext, List<IConsumerMessage>>(context, msgs));
        }
    }
}

