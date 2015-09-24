using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class ChannelConsumerCountor
    {
        static ThreadSafe.Integer channelCount = new ThreadSafe.Integer(0);

        public static int ChannelCount { get { return channelCount.ReadFullFence(); }}

        public static int IncrementChannelCount()
        {
            return channelCount.AtomicIncrementAndGet();
        } 

        public static Dictionary<string,long> ConsumerCount = new Dictionary<string, long>();
        private static object lockObject = new object();
        public static void AddConsumer(string consumer)
        {
            lock (lockObject)
            {
                if (ConsumerCount.ContainsKey(consumer))
                {
                    ConsumerCount[consumer] = ConsumerCount[consumer] + 1;
                }
                else
                {
                    ConsumerCount.Add(consumer, 1);
                }
            }
        }
        public static void RemoveConsumer(string consumer)
        {
            lock(lockObject)
            {
                if (ConsumerCount.ContainsKey(consumer))
                {
                    ConsumerCount[consumer] = ConsumerCount[consumer] - 1;
                }
            }
        }
    }
}
