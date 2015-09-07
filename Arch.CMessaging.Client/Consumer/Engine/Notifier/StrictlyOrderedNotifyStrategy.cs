using System;
using Arch.CMessaging.Client.Core.Message.Retry;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Pipeline;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Utils;
using System.Threading;

namespace Arch.CMessaging.Client.Consumer.Engine.Notifier
{
    public class StrictlyOrderedNotifyStrategy : INotifyStrategy
    {
        private IRetryPolicy m_retryPolicy;

        public StrictlyOrderedNotifyStrategy(IRetryPolicy retryPolicy)
        {
            m_retryPolicy = retryPolicy;
        }

        public void Notify(List<IConsumerMessage> msgs, ConsumerContext context, ProducerConsumer<Action> executorService, IPipeline<object> pipeline)
        {
            foreach (IConsumerMessage msg in msgs)
            {
                NackDelayedBrokerConsumerMessage nackDelayedMsg = new NackDelayedBrokerConsumerMessage((BrokerConsumerMessage)msg);
                List<object> singleMsg = new List<object>();
                singleMsg.Add(nackDelayedMsg);

                int retries = 0;
                while (true)
                {
                    pipeline.Put(new Pair<ConsumerContext, List<object>>(context, singleMsg));

                    if (nackDelayedMsg.BaseConsumerMessage.Status == MessageStatus.FAIL)
                    {
                        // reset status to enable reconsume or nack
                        nackDelayedMsg.BaseConsumerMessage.ResetStatus();
                        if (retries < m_retryPolicy.GetRetryTimes())
                        {
                            Sleep(m_retryPolicy.NextScheduleTimeMillis(retries, 0));
                            retries++;
                        }
                        else
                        {
                            msg.Nack();
                            break;
                        }
                    }
                    else
                    {
                        msg.Ack();
                        break;
                    }
                }
            }
        }

        private void Sleep(long timeMills)
        {
            Thread.Sleep((int)timeMills);
        }
    }
}

