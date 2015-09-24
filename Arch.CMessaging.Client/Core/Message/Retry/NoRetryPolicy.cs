using System;

namespace Arch.CMessaging.Client.Core.Message.Retry
{
    public class NoRetryPolicy : IRetryPolicy
    {
        public int GetRetryTimes()
        {
            return 0;
        }

        public long NextScheduleTimeMillis(int retryTimes, long currentTimeMillis)
        {
            throw new NotImplementedException();
        }
    }
}

