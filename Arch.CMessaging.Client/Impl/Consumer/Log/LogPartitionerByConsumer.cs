using Arch.CMessaging.Core.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Consumer.Log
{
    public class LogPartitionerByConsumer : IPartitioner
    {
        private Dictionary<string, LogLock> locks;
        private Dictionary<string, Wrapper> consumers;
        private NoPartitioner defaultPartitioner;

        public LogPartitionerByConsumer(string[] consumers)
        {
            this.locks = new Dictionary<string, LogLock>();
            this.consumers = new Dictionary<string, Wrapper>();
            this.defaultPartitioner = new NoPartitioner();
            if (consumers != null)
            {
                foreach (var consumer in consumers)
                {
                    this.consumers[consumer.ToString()] = new Wrapper { Consumer = consumer, Hash = ToHex(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(consumer))) };
                    this.locks[consumer] = new LogLock();
                }
            }
        }

        private object lockObject = new object();

        #region IPartitioner Members

        public string GetPartition(string partitionKey)
        {
            Wrapper wrapper = null;
            if (consumers.TryGetValue(partitionKey, out wrapper))
            {
                return wrapper.Hash;
            }
            else
                return defaultPartitioner.GetPartition(partitionKey);
        }

        public LogLock DeclareCriticalArea(string partitionKey)
        {
            LogLock logLock = null;
            Wrapper wrapper = null;
            if (consumers.TryGetValue(partitionKey, out wrapper))
            {
                if (locks.TryGetValue(wrapper.Consumer, out logLock))
                {
                    logLock.DeclareCritical();
                }
            }
            if (logLock == null)
            {
                lock (lockObject)
                {
                    if (consumers.TryGetValue(partitionKey, out wrapper))
                    {
                        if (locks.TryGetValue(wrapper.Consumer, out logLock))
                        {
                            logLock.DeclareCritical();
                        }
                    }
                    if (logLock == null)
                    {
                        this.consumers[partitionKey] = new Wrapper { Consumer = partitionKey, Hash = ToHex(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(partitionKey))) };
                        this.locks[partitionKey] = new LogLock();
                        logLock = this.locks[partitionKey];
                        logLock.DeclareCritical();
                    }
                }
            }
            return logLock ?? defaultPartitioner.DeclareCriticalArea(partitionKey);
        }

        private string ToHex(byte[] bytes)
        {
            return bytes
                .Select(c => c.ToString("X2"))
                .Aggregate((p, n) => p + n)
                .ToLower();
        }

        #endregion

        private class Wrapper
        {
            public string Consumer { get; set; }
            public string Hash { get; set; }
        }
    }
}
