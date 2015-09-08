using System;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Core.Bo
{
    public class Offset
    {
        public long PriorityOffset { get; set; }

        public long NonPriorityOffset{ get; set; }

        public Pair<DateTime, long> ResendOffset{ get; set; }

        public Offset()
            : this(-1, -1, null)
        {
        }

        public Offset(long pOffset, long npOffset, Pair<DateTime, long> rOffset)
        {
            PriorityOffset = pOffset;
            NonPriorityOffset = npOffset;
            ResendOffset = rOffset;
        }

    }
}

