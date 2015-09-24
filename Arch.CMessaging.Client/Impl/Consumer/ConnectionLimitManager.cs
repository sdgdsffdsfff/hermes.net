using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class ConnectionLimitManager
    {
        const int MinConnectionLimit = 10;
        const int MaxConnectionLimit = 1024;
        static ConcurrentDictionary<string,int> dict = new ConcurrentDictionary<string, int>();
        public static void SetConnection(string consumer,int count)
        {
            try
            {
                dict.AddOrUpdate(consumer, count, (k, v) => count);
                int connection;
                if (!dict.TryGetValue(consumer, out connection))
                {
                    dict.TryAdd(consumer, count);
                }
                var c = dict.Values.Sum() + dict.Count;
                if (c > MinConnectionLimit && c < MaxConnectionLimit)
                    ServicePointManager.DefaultConnectionLimit = c;
            }
            catch (Exception ex)
            {
                Logg.Write(ex,LogLevel.Error,"cmessaging.consumer.connectionlimitmanager.setconnection");
            }
        }

        public static void DisposeConnection(string consumer)
        {
            try
            {
                int connection;
                if (dict.TryRemove(consumer, out connection))
                {
                    var c = dict.Values.Sum() + dict.Count;
                    if (c > MinConnectionLimit)
                        ServicePointManager.DefaultConnectionLimit = c;
                }
            }
            catch (Exception ex)
            {
                Logg.Write(ex,LogLevel.Error,"cmessaging.consumer.connectionlimitmanager.disposeconnection");
            }
        }
    }
}
