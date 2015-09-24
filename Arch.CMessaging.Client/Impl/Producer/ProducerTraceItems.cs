using System;
using System.Net;
using Arch.CFramework.AppInternals.Configuration.Bean;
using Arch.CMessaging.Core.Log;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class ProducerTraceItems : ConfigBeanBase
    {
        public ProducerTraceItems() : base(true) { }
        public string Servers { get; set; }
        public string AdminUrl { get; set; }
        public string ProducerConfigUrl { get; set; }
        public int ProducerTimeout { get; set; }
        public int ConnectionMaxCount { get; set; }
        public int ConnectionCount { get; set; }
        public int ConnectionLimit { get { return ServicePointManager.DefaultConnectionLimit; } }

        protected override void Load()
        {
        }

        private static ProducerTraceItems trace;
        private static readonly object LockObject = new object();

        public static ProducerTraceItems Instance
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
                                trace = ConfigBeanManager.Current.GetConfigBean(typeof(ProducerTraceItems).Name) as ProducerTraceItems;
                                if (trace == null)
                                {
                                    trace = new ProducerTraceItems();
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
                                trace = new ProducerTraceItems();
                            }
                        }
                    }
                    Logg.Write(ex, LogLevel.Error, "cmessaging.producer.producertraceitems");
                }
                return trace;
            }
        }
    }
}
