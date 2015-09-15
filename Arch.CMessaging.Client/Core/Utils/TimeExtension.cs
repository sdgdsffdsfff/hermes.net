using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch.CMessaging.Client.Core.Utils
{
    public static class TimeExtension
    {

        public static long EPOCH_TICKS = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public static long CurrentTimeMillis(this DateTime time)
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static long UnixTimestampToTicks(long timestamp)
        {
            return EPOCH_TICKS + timestamp * TimeSpan.TicksPerMillisecond;
        }
    }
}
