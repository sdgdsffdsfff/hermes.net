using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Util;
using cmessaging.producer.sync;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class DefaultMessageChannel : IProducerChannel
    {
        public DefaultMessageChannel()
        {
            ObjectFactoryLifetimeManager.Instance.Register();
            CollectorServiceUris = new string[0];
        }
        private ThreadSafe.Boolean isOpen;
        private RemoteServerReader serverReader = RemoteServerReader.GetInstance();
        private IScheduler SyncScheduler = null;
        private List<string> exchanges = new List<string>();//EXCHANGE列表，用于V1.0 adminservice
        private List<string> producers = new List<string>();//PRODUCER列表,用于V0.9 admin
        private object lockObject = new object();
        private ConcurrentDictionary<string, List<string>> exchangeCollects = new ConcurrentDictionary<string, List<string>>();//V1.0Collect列表
        private string[] CollectorServiceUris;//V0.9Collect 列表

        #region 属性
        public event ChannelOutOfCapacityEventHandler OutOfCapacity = delegate { };
        public ushort ConnectionMax { get; set; }
        public bool EnableMonitoring { get; set; }
        public int AckTimeout { get; set; }
        public uint Capacity { get; set; }
        public IDictionary ClientProperties { get; private set; }
        public IDictionary ServerProperties { get; private set; }
        public uint CurrentSize { get; private set; }
        public bool IsReliable { get; private set; }
        public bool IsInOrder { get; private set; }
        public string Uri { get; private set; }
        public string[] KnownHosts { get; private set; }

        public bool IsOpen
        {
            get { return isOpen.ReadAcquireFence(); }
        }

        private bool IsHasOpen
        {
            get { return (!isOpen.AtomicCompareExchange(true, false)); }
        }
        //是否不存在FXCONFIG
        private bool IsNoExistFxConfig
        {
            get
            {
                var serviceUrl = ConfigurationManager.AppSettings["FxConfigServiceUrl"];
                return string.IsNullOrEmpty(serviceUrl);
            }
        }
        #endregion 

        public IMessageProducer CreateProducer()
        {
            Open(1000);
            return new DefaultMessageProducer {Channel = this};
        }

        public void Open(int timeout)
        {
            //已OPEN的，不再OPEN，多线程处理
            if (IsHasOpen) return;
            if (ConfigUtil.Instance.ConnectionCount > 0)
                ServicePointManager.DefaultConnectionLimit = ConfigUtil.Instance.ConnectionCount;
            ConfigUtil.Instance.NotifyPropertyChange += (type) =>
                                                            {
                                                                switch (type)
                                                                {
                                                                    case NotifyProperty.ConnectionCount:
                                                                        if (ConfigUtil.Instance.ConnectionCount > 0)
                                                                            ServicePointManager.DefaultConnectionLimit =
                                                                                ConfigUtil.Instance.ConnectionCount;
                                                                        break;
                                                                    default:
                                                                        break;
                                                                }
                                                            };
            if (IsNoExistFxConfig)
            {
                IDictionary<string, string> settings = SettingsUtils.GetAppSettings();
                string configServiceUri = "";
                if (settings.ContainsKey("CmessageServiceUri"))
                    configServiceUri = settings["CmessageServiceUri"];
                if (string.IsNullOrEmpty(configServiceUri))
                {
                    throw new Exception("Must config 'CmessageServiceUri' OR 'FxConfigServiceUrl' in appSettings.");
                }
            }
            if(SyncScheduler==null)
            {
                SyncScheduler = ObjectFactory.Current.Get<IScheduler>(Lifetime.ContainerControlled);
                SyncScheduler.Register(SyncServers, "cmessaging.producer.defaultmessagechannel.syncconfig", 60 * 1000, false);
            }
            if(!IsNoExistFxConfig)
            {
                ConfigUtil.Instance.RunScheduler();//v1.0启动同步
            }
        }

        public void Close(int timeout)
        {
            isOpen.AtomicExchange(false);
        }
        //同步服务
        private void SyncServers()
        {
            int isSuccess = 1;
            try
            {
                if (IsNoExistFxConfig)
                {
                    if (producers.Count < 1) return;
                    //v0.9
                    IDictionary<string, string> settings = SettingsUtils.GetAppSettings();
                    var configServiceUri = settings["CmessageServiceUri"];
                    if(string.IsNullOrEmpty(configServiceUri))
                    {
                        throw new Exception("Must config 'CmessageServiceUri' OR 'FxConfigServiceUrl' in appSettings.");
                    }
                    var currentCollectorServiceUris = serverReader.GetCollectorService(configServiceUri, string.Join("|", producers));
                    if (currentCollectorServiceUris != null && currentCollectorServiceUris.Length > 0)
                    {
                        CollectorServiceUris = currentCollectorServiceUris;
                    }
                }
                else
                {
                    //v1.0
                    var arr = exchanges.ToArray();
                    if (arr.Length < 1) return;
                    var str = string.Join(",", arr);
                    var exchangeServices = serverReader.GetCollects(str);
                    
                    var dict = new Dictionary<string, List<string>>();
                    foreach (var exchangePhysicalServer in exchangeServices)
                    {
                        var service = exchangePhysicalServer;
                        if(dict.ContainsKey(service.ExchangeName))
                        {
                            if (!dict[service.ExchangeName].Contains(service.ServerDomainName))
                                dict[service.ExchangeName].Add(service.ServerDomainName);
                        }
                        else
                        {
                            dict.Add(service.ExchangeName, new List<string> { service.ServerDomainName });
                        }
                    }
                    string servers = "";
                    foreach (var item in dict.Keys)
                    {
                        var exchange = item;
                        exchangeCollects.AddOrUpdate(exchange, s => dict[exchange], (s, list) => dict[exchange]);
                        servers += string.Format("{0}[{1}],", exchange, string.Join(",", dict[exchange]));
                    }
                    ProducerTraceItems.Instance.Servers = servers;
                }
            }
            catch (Exception ex)
            {
                isSuccess = 0;
                Logg.Write(ex, LogLevel.Error, "consumer.producer.defaultmessagechannel.syncservers");
            }
            finally
            {
                MetricManagerFactory.MetricManager.Set(new SyncCountMetric { Type = "server",IsSuccess = isSuccess.ToString()});
            }
        }
        //注册Exchange
        public void RegisterExchange(string exchange,string identifier)
        {
            var producer = exchange + ":" + identifier;
            lock (lockObject)
            {
                if(!producers.Contains(producer))producers.Add(producer);
            }
            if (IsNoExistFxConfig && CollectorServiceUris.Length > 0) return;//运行V0.9及服务器地址存在，直接把返回
            if (exchanges.Contains(exchange)) return;
            lock (lockObject)
            {
                if (exchanges.Contains(exchange)) return;
                exchanges.Add(exchange);
            }
            SyncServers();
        }
        //获取Collector服务列表
        internal  string[] GetCollects(string exchange)
        {
            if(IsNoExistFxConfig)
            {
                //V0.9
                if(CollectorServiceUris.Length<1)
                    SyncServers();
                return CollectorServiceUris;
            }
            //V1.0
            List<string> collects;
            exchangeCollects.TryGetValue(exchange, out collects);
            if(collects == null || collects.Count<1)
            {
                if (!exchanges.Contains(exchange))
                    exchanges.Add(exchange);
                SyncServers();
            }
            return exchangeCollects.TryGetValue(exchange, out collects) ? collects.ToArray() : new string[0];
        }

        public void Dispose()
        {
            Close(1000);
        }
    }
}
