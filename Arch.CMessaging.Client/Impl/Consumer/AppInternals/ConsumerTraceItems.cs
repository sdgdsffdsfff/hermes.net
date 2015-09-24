using System;
using System.Net;
using Arch.CFramework.AppInternals.Configuration.Bean;
using Arch.CMessaging.Core.Log;

namespace Arch.CMessaging.Client.Impl.Consumer.AppInternals
{
    public class ConsumerTraceItems : ConfigBeanBase
    {
        public ConsumerTraceItems() : base(true) { }
        public string Servers { get; set; }
        public string AdminUrl { get; set; }

        public uint BatchSize { get; set; }
        /// <summary>
        /// 默认最大连接数
        /// </summary>
        public uint ReceiveTimeout { get; set; }
        /// <summary>
        /// 最大连接数
        /// </summary>
        public ushort ConnectionMax { get; set; }
        public int AckTimeout { get; set; }
        public int Timeout { get; set; }
        public uint Capacity { get; set; }
        public int TopicCount { get; set; }
        public int MarkServerRemove { get; set; }
        public int MaxAckTimeout { get; set; }
        public string ConsumerReceiveTimeout { get; set; }
        public string ConsumerBatchSize { get; set; }
        public string ConsumerPoolSize { get; set; }

        public int ConnectionLimit { get { return ServicePointManager.DefaultConnectionLimit; } }

        protected override void Load()
        {
            //BatchSize =0;
            //ReceiveTimeout =0;
            //ConnectionMax =0;
        }

        private static ConsumerTraceItems trace;
        private static readonly object LockObject = new object();

        public static ConsumerTraceItems Instance
        {
            get
            {
                try
                {
                    if (trace == null)
                    {
                        lock (LockObject)
                        {
                            if (trace == null)
                            {
                                trace = ConfigBeanManager.Current.GetConfigBean(typeof(ConsumerTraceItems).Name) as ConsumerTraceItems;
                                if (trace == null)
                                {
                                    trace = new ConsumerTraceItems();
                                    ConfigBeanManager.Current.Register(trace);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (trace == null)
                    {
                        lock (LockObject)
                        {
                            if (trace == null)
                            {
                                trace = new ConsumerTraceItems();
                            }
                        }
                    }
                    Logg.Write(ex, LogLevel.Error, "consumer.consumertraceitems");
                }
                return trace;
            }
        }
    }
}
