using System;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Message;

namespace Arch.CMessaging.Client.Consumer.Api
{
    public interface IMessageStream
    {
        int GetParatitionId();

        List<IConsumerMessage> FetchMessages(long offset, int size);

        List<IConsumerMessage> FetchMessages(List<long> offsets);

        // Long.MIN_VALUE返回最早的offset，Long.MAX_VALUE返回最新的offset
        long GetOffsetByTime(long time);
    }
}

