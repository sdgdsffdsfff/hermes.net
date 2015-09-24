using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using cmessaging.consumer.channel;
using cmessaging.consumer.exception;
using cmessaging.consumer.sync;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public sealed class ConsumerChannel : IConsumerChannel
    {
        private ThreadSafe.Boolean _isOpen;
        readonly List<string> _exchangeList = new List<string>();
        //hostname,physicalserver
        ConcurrentDictionary<string, PhysicalServer> _hostServers = new ConcurrentDictionary<string, PhysicalServer>();
        internal InputBuffer Input { get;private set; }
        internal OutputBuffer Output { get;private set; }
        private IService Service { get; set; }
        private IScheduler SyncServerScheduler;
        private static IScheduler CountorScheduler;
        public ConsumerChannel(string uri, bool isReliable, bool isInOrder):this(uri, isReliable, isInOrder, new DefaultService(new DefaultClient()))
        {
        }

        internal ConsumerChannel(string uri, bool isReliable, bool isInOrder, IService service)
        {
            try
            {
                ObjectFactoryLifetimeManager.Instance.Register();
                ChannelConsumerCountor.IncrementChannelCount();

                Uri = uri;
                IsReliable = isReliable;
                IsInOrder = isInOrder;
                Service = service;
                _isOpen = new ThreadSafe.Boolean(false);
                Output = new OutputBuffer(service);
                Input = new InputBuffer(service);

                setAckTimeout(ConfigUtil.Instance.AckTimeout);
                setCapacity(ConfigUtil.Instance.Capacity);
                setConnectionMax(ConfigUtil.Instance.ConnectionMax);

                ConfigUtil.Instance.RunScheduler();
                ConfigUtil.Instance.NotifyPropertyChange += OnNotifyPropertyChange;

                if (SyncServerScheduler == null)
                {
                    SyncServerScheduler = ObjectFactory.Current.Get<IScheduler>(Lifetime.ContainerControlled);
                    SyncServerScheduler.Register(SyncServers, "consumer.channelconsumer.syncservers", Consts.Consumer_ServerSyncIntervalTime);
                }
                if (CountorScheduler == null)
                {
                    CountorScheduler = ObjectFactory.Current.Get<IScheduler>(Lifetime.ContainerControlled);
                    CountorScheduler.Register(RecordChannelConsumerCountor, "consumer.channelconsumer.countor", 60 * 1000);
                }
            }
            catch (Exception ex)
            {
                MetricUtil.Set(new ExceptionCountMetric { HappenedWhere = ExceptionType.OnChannelCreate.ToString() });
                Logg.Write(ex,LogLevel.Error, "consumer.consuemrchannel");
            }
        }

        public event ChannelOutOfCapacityEventHandler OutOfCapacity;

        /// <summary>
        /// 创建Consumer
        /// </summary>
        /// <typeparam name="TMessageConsumer"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public TMessageConsumer CreateConsumer<TMessageConsumer>() where TMessageConsumer : IMessageConsumer
        {
            ConstructorInfo constructor;
            Guard.TypeHasConstructor(typeof(TMessageConsumer), typeof(TMessageConsumer).FullName, new[] { typeof(IConsumerBuffer) }, out constructor);

            Open(Consts.Consumer_DefaultConnectTimeout);
            return (TMessageConsumer)Activator.CreateInstance(typeof(TMessageConsumer), new ConsumerBuffer(this));
        }

        #region 属性

        public IDictionary ClientProperties
        {
            get { return new Dictionary<string, string>(); }
        }

        public IDictionary ServerProperties { get; private set; }

        public bool EnableMonitoring { get; set; }

        private bool _setConnectionMax = false;
        private ushort _connectionMax;
        /// <summary>
        /// 
        /// </summary>
        public ushort ConnectionMax
        {
            get { return _connectionMax; }
            set
            {
                if (value != _connectionMax && value > 0)
                {
                    _setConnectionMax = true;
                    setConnectionMax(value);
                }
            }
        }
        private void setConnectionMax(ushort value)
        {
            _connectionMax = value;
            Input.ConnectionMax = value;
            ConsumerTraceItems.Instance.ConnectionMax = value;
        }

        private bool _setAckTimeout =false;
        private int _ackTimeout;
        public int AckTimeout
        {
            get { return _ackTimeout; }
            set
            {
                if (value < 1) return;
                if (value > ConfigUtil.Instance.MaxAckTimeout) return;
                _setAckTimeout = true;
                setAckTimeout(value);
            }
        }
        private void setAckTimeout(int value)
        {
            _ackTimeout = value;
            Input.AckTimeout = value;
            ConsumerTraceItems.Instance.AckTimeout = value;
        }

        private bool _setCapacity = false;
        private uint _capacity; 
        public uint Capacity
        {
            get { return _capacity; }
            set
            {
                if (value <= 0) return;
                _setCapacity = true;
                setCapacity(value);
            }
        }
        private void setCapacity(uint value)
        {
            _capacity = value;
            Input.Capacity = value;
            ConsumerTraceItems.Instance.Capacity = value;
        }

        public uint CurrentSize
        {
            get { return (uint)Input.MomoryManager.CurrentMemorySize; }
        }

        public bool IsReliable { get; private set; }

        public bool IsInOrder { get; private set; }

        public string Uri{ get; private set; }

        public string[] KnownHosts { get; private set; }

        public bool IsOpen
        {
            get { return _isOpen.ReadAcquireFence(); }
        }

        private bool IsHasOpen
        {
            get
            {
                return (!_isOpen.AtomicCompareExchange(true, false));
            }
        }
        #endregion

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="timeout">毫秒</param>
        public void Open(int timeout)
        {
            //已OPEN的，不再OPEN，多线程处理
            if (IsHasOpen) return;
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="timeout"></param>
        public void Close(int timeout)
        {
            _isOpen.AtomicExchange(false);
            Input.RegisterServer(null);
        }

        public void Dispose()
        {
            if (Input != null) Input.Dispose();
            if (Output != null) Output.Dispose();
            Close(Consts.Consumer_DefaultConnectTimeout);
        }
        //根据HostName获取SERVERURI
        public PhysicalServer GetPhysicalServer(string serverHostName)
        {
            if (serverHostName == null) return null;
            PhysicalServer server;
            _hostServers.TryGetValue(serverHostName.ToLower().Trim(), out server);
            return server;
        }

        //向CONSUMER列表中添加 identifier
        public void GetExchangePhysicalServers(string pullingRequestUri)
        {
            if (string.IsNullOrEmpty(pullingRequestUri)) return;
            string exchange = Input.GetExchange(pullingRequestUri);
            Input.RegisterConsumer(exchange,pullingRequestUri);
            if (_exchangeList.Contains(exchange)) return;
            _exchangeList.Add(exchange);
            RefreshExchangePhysicalServers();
        }

        /// <summary>
        /// 更新服务器列表
        /// </summary>
        private void RefreshExchangePhysicalServers(int timeout = 0)
        {
            try
            {
                if (!IsOpen) return;
                if (_exchangeList.Count < 1) return;
                var exchanges = string.Join(",", _exchangeList);
                var exchangeServers = Service.GetExchangePhysicalServers(exchanges, timeout);
                if (exchangeServers == null || exchangeServers.Count < 1)
                {
                    Logg.Write("没有服务器列表", LogLevel.Warn, "consumer.consumerchannel.refreshservers",
                                 new[] {new KeyValue {Key = "exchanges", Value = exchanges}});
                    exchangeServers = new List<ExchangePhysicalServer>();
                }
                KnownHosts = (from s in exchangeServers
                              select s.ServerDomainName).ToArray();

                var dict = exchangeServers
                    .GroupBy(x => x.ExchangeName)
                    .ToDictionary(x => x.Key,
                                  y => y.ToList());
                ServerProperties = dict;
                Input.RegisterServer(dict);

                foreach (var exchangeServer in exchangeServers)
                {
                    var server = exchangeServer;
                    _hostServers.AddOrUpdate(server.ServerName.ToLower().Trim(),
                                         s =>
                                         new PhysicalServer
                                             {
                                                 ServerDomainName = server.ServerDomainName,
                                                 ServerIP = server.ServerIP,
                                                 ServerName = server.ServerName
                                             },
                                         (s, server1) =>
                                         new PhysicalServer
                                             {
                                                 ServerDomainName = server.ServerDomainName,
                                                 ServerIP = server.ServerIP,
                                                 ServerName = server.ServerName
                                             });
                }

                var sb = new StringBuilder();
                foreach (var exchangeServer in dict)
                {
                    sb.Append(exchangeServer.Key + ":");
                    foreach (var server in exchangeServer.Value)
                    {
                        sb.AppendFormat("{0}[{1}],", server.ServerName, server.Weight);
                    }
                    sb.Append("|");
                }
                ConsumerTraceItems.Instance.Servers = sb.ToString();
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.consumerchannel.refreshservers");
            }
        }

        public void OnNotifyPropertyChange(NotifyProperty property)
        {
            switch (property)
            {
                case NotifyProperty.Capacity:
                    if (_setCapacity) break;
                    setCapacity(ConfigUtil.Instance.Capacity);
                    break;
                case NotifyProperty.ConnectionMax:
                    if (_setConnectionMax) break;
                    setConnectionMax(ConfigUtil.Instance.ConnectionMax);
                    break;
                case NotifyProperty.AckTimeout:
                    if (_setAckTimeout) break;
                    setAckTimeout(ConfigUtil.Instance.AckTimeout);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 记录channel consumer的metric信息
        /// </summary>
        private void RecordChannelConsumerCountor()
        {
            try
            {
                MetricUtil.Set(new ChannelCountMetric(), ChannelConsumerCountor.ChannelCount);
                var consumers = ChannelConsumerCountor.ConsumerCount.Keys.ToArray();
                foreach (var s in consumers)
                {
                    MetricUtil.Set(new ConsumerCountMetric { consumer = s }, ChannelConsumerCountor.ConsumerCount[s]);
                }
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.consumerchannel.recordchannelconsumercountor");
            }
        }
        /// <summary>
        /// 同步服务器地址
        /// </summary>
        private void SyncServers()
        {
            try
            {
                MetricUtil.Set(new SyncCountMetric { Type = "server" });
                RefreshExchangePhysicalServers();
            }
            catch (Exception exception)
            {
                var exchanges = string.Join(",", _exchangeList);
                Logg.Write(exception, LogLevel.Error,
                             "consumer.consumerchannel.syncservers",
                             new KeyValue { Key = "Exchanges", Value = exchanges });
            }
        }
    }
}
