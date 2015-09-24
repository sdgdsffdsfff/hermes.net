using System;
using Arch.CMessaging.Client.Core.Message.Retry;

namespace Arch.CMessaging.Client.Consumer.Api
{
    public class MessageListenerConfig
    {
        private IRetryPolicy retryPolicy;

        public StrictlyOrderingRetryPolicy StrictlyOrderingRetryPolicy
        { 
            set
            {
                retryPolicy = value.ToRetryPolicy();
                StrictlyOrdering = true;
            }
        }

        public IRetryPolicy GetStrictlyOrderingRetryPolicy()
        {
            return retryPolicy;
        }

        public bool StrictlyOrdering { get; private set; }

        public MessageListenerConfig()
        {
            StrictlyOrdering = false;
        }
    }

    public abstract class StrictlyOrderingRetryPolicy
    {
        private StrictlyOrderingRetryPolicy()
        {
        }

        public static StrictlyOrderingRetryPolicy EvenRetry(int retryIntervalMills, int retryTimes)
        {
            return new EvenRetryPolicy(retryIntervalMills, retryTimes);
        }

        public abstract IRetryPolicy ToRetryPolicy();

        class EvenRetryPolicy : StrictlyOrderingRetryPolicy
        {

            private IRetryPolicy retryPolicy;

            public EvenRetryPolicy(int retryIntervalMills, int retryTimes)
            {
                retryPolicy = new InnerEvenRetryPolicy(retryIntervalMills, retryTimes);
            }

            public override IRetryPolicy ToRetryPolicy()
            {
                return retryPolicy;
            }

            class InnerEvenRetryPolicy : IRetryPolicy
            {

                private int retryIntervalMills;

                private int retryTimes;

                public InnerEvenRetryPolicy(int retryIntervalMills, int retryTimes)
                {
                    this.retryIntervalMills = retryIntervalMills;
                    this.retryTimes = retryTimes;
                }

                public long NextScheduleTimeMillis(int retryTimes, long currentTimeMillis)
                {
                    return currentTimeMillis + retryIntervalMills;
                }

                public int GetRetryTimes()
                {
                    return retryTimes;
                }
            }

        }
    }
}

