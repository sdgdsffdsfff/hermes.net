using System;
using System.Collections.Concurrent;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal  sealed class PullingErrorManager
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<int, DateTime>>> _dc = new ConcurrentDictionary<string, ConcurrentDictionary<string, Tuple<int, DateTime>>>();
        private Random random;
        public PullingErrorManager()
        {
            long tick = DateTime.Now.Ticks;
            random = new Random((int)(tick & 0xffffffffL));
        }
        
        public void Block(string uri, string serverUri, bool sync = true)
        {
            if (sync)
            {
                //同步直接休眠
                System.Threading.Thread.Sleep(RandomTimes());
            }
            else
            {
                //异步 根据上次错误时间进行BLOCK
                ConcurrentDictionary<string, Tuple<int, DateTime>> value;
                if (_dc.TryGetValue(uri, out value))
                {
                    Tuple<int, DateTime> tuple;
                    if(value.TryGetValue(serverUri, out tuple))
                    {
                        //死信多休眠一会。
                        var milliseconds = (serverUri.StartsWith("dead:")) ? RandomLongTimes() : RandomTimes();

                        var totalMilliseconds = (int)((DateTime.Now - tuple.Item2).TotalMilliseconds);
                        if(totalMilliseconds<milliseconds)
                        {
                            System.Threading.Thread.Sleep(milliseconds - totalMilliseconds);
                        }
                    }
                }
            }
        }
        //设置
        public void Set(string uri, string serverUri, bool hasMessages, DateTime dt)
        {
            if(hasMessages)
            {
                ConcurrentDictionary<string, Tuple<int, DateTime>> value;
                if (_dc.TryGetValue(uri, out value))
                {
                    Tuple<int, DateTime> tuple;
                    value.TryRemove(serverUri, out tuple);
                }
            }
            else
            {
                //没值
                var value = _dc.GetOrAdd(uri, new ConcurrentDictionary<string, Tuple<int, DateTime>>());
                value.AddOrUpdate(serverUri,s => new Tuple<int, DateTime>(1,dt),(s,d) => new Tuple<int, DateTime>(d.Item1+1,dt));
            }
        }
        /// <summary>
        /// 500~1000毫秒
        /// </summary>
        /// <returns></returns>
        private int RandomTimes()
        {
            return random.Next(500, 1000);
        }
        /// <summary>
        /// 5~10 秒
        /// </summary>
        /// <returns></returns>
        private int RandomLongTimes()
        {
            return random.Next(5000, 10000);
        }
    }
}
