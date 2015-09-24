using System;
using System.Collections.Concurrent;
using System.Configuration;
using Arch.CFramework.Configuration;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using System.Collections.Generic;
using cmessaging.consumer;
using cmessaging.consumer.exception;
using cmessaging.consumer.sync;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class ConfigUtil
    {
        private readonly static ConfigUtil _instance = new ConfigUtil();
        public static ConfigUtil Instance
        {
            get { return _instance; }
        }

        public delegate void NotifyPropertyChangeDelegate(NotifyProperty property);
        public NotifyPropertyChangeDelegate NotifyPropertyChange;

        private readonly Queue<string> _consumerQueue = new Queue<string>();
        private readonly ConcurrentDictionary<string, uint> _batchSizeDict = new ConcurrentDictionary<string, uint>();
        private readonly ConcurrentDictionary<string, uint> _receiveTimeoutDict = new ConcurrentDictionary<string, uint>();
        private readonly ConcurrentDictionary<string, int> _poolSizeDict = new ConcurrentDictionary<string, int>();

        public void RegisterConsumer(string consumer)
        {
            if (!_consumerQueue.Contains(consumer))
            {
                _consumerQueue.Enqueue(consumer);
            }
        }

        private IScheduler SyncMetadataScheduler;
        public void RunScheduler()
        {
            if (SyncMetadataScheduler != null) return;
            SyncMetadataScheduler = ObjectFactory.Current.Get<IScheduler>(Lifetime.ContainerControlled);
            //每隔一分钟刷新一次
            SyncMetadataScheduler.Register(() =>
            {
                try
                {
                    var fxConfigServiceUrl = ConfigurationManager.AppSettings["FxConfigServiceUrl"];
                    if (string.IsNullOrEmpty(fxConfigServiceUrl))
                    {
                        MetricUtil.Set(new ExceptionCountMetric() { Consumer = "", HappenedWhere = ExceptionType.OnMetadataSync.ToString() });
                        return;
                    }
                    MetricUtil.Set(new SyncCountMetric { Type = "metadata" });

                    CmessagingAdminUrl = CentralConfig.GetValue(Consts.Consumer_Config_AdminUrl);
                    TopicCount = Get(Consts.Consumer_Config_TopicCount, Consts.Consumer_TopicCount, int.Parse);
                    MaxAckTimeout = Get(Consts.Consumer_Config_MaxAckTimeout, Consts.Consumer_MaxAckTimeout, int.Parse);
                    MarkServerRemove = Get(Consts.Consumer_Config_MarkServerRemove, Consts.Consumer_MarkServerRemove, int.Parse);
                    Capacity = Get(Consts.Consumer_Config_Capacity, Consts.Consumer_DefaultCapacity, uint.Parse);
                    ConnectionMax = Get(Consts.Consumer_Config_ConnectionMax, Consts.Consumer_DefaultConnectionMax, ushort.Parse);
                    AckTimeout = Get(Consts.Consumer_Config_AckTimeout, Consts.Consumer_AckTimeout, int.Parse);
                    Timeout = Get(Consts.Consumer_Config_Timeout, Consts.Consumer_Timeout, int.Parse);
                    BatchSize = Get(Consts.Consumer_Config_BatchSize, (uint)Consts.Consumer_DefaultBatchSize, uint.Parse);
                    ReceiveTimeout = Get(Consts.Consumer_Config_ReceiveTimeout, Consts.Consumer_DefaultReceiveTimeout, uint.Parse);


                    var sbBatchSize = new StringBuilder();
                    var sbReceiveTimeout = new StringBuilder();
                    var sbPoolSize = new StringBuilder();
                    var consumers = _consumerQueue.ToArray();
                    
                    foreach (var consumer in consumers)
                    {
                        
                        #region batchsize
                        uint size;
                        uint.TryParse(CentralConfig.GetValue(string.Format("{0}_{1}",Consts.Consumer_Config_BatchSize, consumer)), out size);
                        if (size > 0)
                        {
                            sbBatchSize.AppendFormat("[{0}:{1}]", consumer, size);
                            var isNotify = false;
                            _batchSizeDict.AddOrUpdate(consumer,
                                                      x =>
                                                      {
                                                          isNotify = true;
                                                          return size;
                                                      },
                                                       (x, s) =>
                                                       {
                                                           if (s != size)
                                                               isNotify = true;
                                                           return size;
                                                       });
                            if (isNotify) NotifyPropertyChange(NotifyProperty.ReceiveTimeout);
                        }
                        else
                        {
                            uint tempBatchSize;
                            _batchSizeDict.TryRemove(consumer, out tempBatchSize);
                        }
                        #endregion 

                        #region receivetimeout
                        uint consumerReceiveTimeout;
                        uint.TryParse(CentralConfig.GetValue(string.Format("{0}_{1}",Consts.Consumer_Config_ReceiveTimeout, consumer)), out consumerReceiveTimeout);
                        if (consumerReceiveTimeout > 0)
                        {
                            sbReceiveTimeout.Append(string.Format("[{0}:{1}]", consumer, consumerReceiveTimeout));
                            var isNotify = false;
                            _receiveTimeoutDict.AddOrUpdate(consumer,
                                                      x =>
                                                      {
                                                          isNotify = true;
                                                          return consumerReceiveTimeout;
                                                      },
                                                       (x, s) =>
                                                       {
                                                           if (s != consumerReceiveTimeout)
                                                               isNotify = true;
                                                           return consumerReceiveTimeout;
                                                       });
                            if (isNotify) NotifyPropertyChange(NotifyProperty.ReceiveTimeout);
                        }
                        else
                        {
                            uint tempBatchSize;
                            _receiveTimeoutDict.TryRemove(consumer, out tempBatchSize);
                        }
                        #endregion 

                        #region poolSize

                        int poolSize;
                        int.TryParse(CentralConfig.GetValue(string.Format("{0}_{1}", Consts.Consumer_Config_PoolSize, consumer)),out poolSize);
                        if (poolSize > 0)
                        {
                            sbPoolSize.Append(string.Format("[{0}:{1}]", consumer, poolSize));
                            var isNotify = false;
                            _poolSizeDict.AddOrUpdate(consumer,x=>{
                                isNotify = true;
                                return poolSize;
                            },(x,s)=>{
                                if(s !=poolSize){
                                    isNotify = true;
                                }
                                return poolSize;
                            });
                            if (isNotify) NotifyPropertyChange(NotifyProperty.PoolSize);
                        }
                        else
                        {
                            int tmpPoolSize;
                            _poolSizeDict.TryRemove(consumer, out tmpPoolSize);
                        }
                        #endregion 
                    }
                    ConsumerTraceItems.Instance.ConsumerBatchSize = sbBatchSize.ToString();
                    ConsumerTraceItems.Instance.ConsumerReceiveTimeout = sbReceiveTimeout.ToString();
                    ConsumerTraceItems.Instance.ConsumerPoolSize = sbPoolSize.ToString();
                }
                catch (Exception ex)
                {
                    Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.runscheduler");
                }
            }, "ConfigScheduler", 60 * 1000, true);
        }

        public T Get<T>(string key, T defaultValue, Converter<string, T> convertHandler)
        {
            var str = CentralConfig.GetValue(key);
            if (string.IsNullOrWhiteSpace(str))
            {
                return defaultValue;
            }
            else 
            {
                try
                {
                    var arr = Array.ConvertAll<string, T>(new string[] { str.Trim() }, convertHandler);
                    return arr[0];
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        private string _cmessagingAdminUrl;
        /// <summary>
        /// 服务器地址
        /// </summary>
        public string CmessagingAdminUrl
        {
            get
            {
                if (string.IsNullOrEmpty(_cmessagingAdminUrl))
                {
                    try
                    {
                        _cmessagingAdminUrl = CentralConfig.GetValue(Consts.Consumer_Config_AdminUrl);
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.cmessagingadminurl");
                    }
                }
                return _cmessagingAdminUrl;
            }
            private set
            {
                if (string.IsNullOrEmpty(value) || value == _cmessagingAdminUrl) return;
                _cmessagingAdminUrl = value;
            }
        }

        private int _topicCount;
        /// <summary>
        /// 最大ACK超时时间
        /// </summary>
        public int TopicCount
        {
            get
            {
                if (_topicCount < 1)
                {
                    try
                    {
                        int temp;
                        int.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_TopicCount), out temp);
                        _topicCount = temp > 0 ? temp : Consts.Consumer_TopicCount;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.topiccount");
                        _topicCount = Consts.Consumer_TopicCount;
                    }
                }
                ConsumerTraceItems.Instance.TopicCount = _topicCount;
                return _topicCount;
            }
            private set
            {
                if (_topicCount < 1 || _topicCount == value) return;
                _topicCount = value;
                ConsumerTraceItems.Instance.TopicCount = _topicCount;
            }
        }

        private int _maxAckTimeout;
        /// <summary>
        /// 最大ACK超时时间
        /// </summary>
        public int MaxAckTimeout
        {
            get
            {
                if (_maxAckTimeout < 1)
                {
                    try
                    {
                        int temp;
                        int.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_MaxAckTimeout), out temp);
                        _maxAckTimeout = temp > 0 ? temp : Consts.Consumer_MaxAckTimeout;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.maxacktimeout");
                        _maxAckTimeout = Consts.Consumer_MaxAckTimeout;
                    }
                }
                ConsumerTraceItems.Instance.MaxAckTimeout = _maxAckTimeout;
                return _maxAckTimeout;
            }
            private set
            {
                if (value <= 0 || value == _maxAckTimeout) return;
                _maxAckTimeout = value;
                ConsumerTraceItems.Instance.MaxAckTimeout = _maxAckTimeout;
            }
        }

        private int _markServerRemove;
        /// <summary>
        /// MARK SERVER连续多数未取到数据，从服务器队列中移除
        /// </summary>
        public int MarkServerRemove
        {
            get
            {
                try
                {
                    int temp;
                    int.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_MarkServerRemove), out temp);
                    _markServerRemove = temp > 0 ? temp : Consts.Consumer_MarkServerRemove;
                }
                catch (Exception ex)
                {
                    Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.markserverremove");
                    _markServerRemove = Consts.Consumer_MarkServerRemove;
                }
                ConsumerTraceItems.Instance.MarkServerRemove = _markServerRemove;
                return _markServerRemove;
            }
            private set
            {
                if (value <= 0 || value == _markServerRemove) return;
                _markServerRemove = value;
                ConsumerTraceItems.Instance.MarkServerRemove = _markServerRemove;
            }
        }

        private uint _capacity;
        /// <summary>
        /// 内存大小
        /// </summary>
        public uint Capacity
        {
            get
            {
                if (_capacity < 1)
                {
                    try
                    {
                        uint temp;
                        uint.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_Capacity), out temp);
                        _capacity = temp > 0 ? temp : Consts.Consumer_DefaultCapacity;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.capacity");
                        _capacity = Consts.Consumer_DefaultCapacity;
                    }
                }
                ConsumerTraceItems.Instance.Capacity = _capacity;
                return _capacity;
            }
            private set
            {
                if (value <= 0 || value == _capacity) return;
                _capacity = value;
                ConsumerTraceItems.Instance.Capacity = _capacity;
                NotifyPropertyChange(NotifyProperty.Capacity);
            }
        }

        private ushort _connectionMax;
        /// <summary>
        /// 连接DISPATCHER池最大连接数
        /// </summary>
        public ushort ConnectionMax
        {
            get
            {
                if (_connectionMax < 1)
                {
                    try
                    {
                        ushort temp;
                        ushort.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_ConnectionMax), out temp);
                        _connectionMax = temp > 0 ? temp : Consts.Consumer_DefaultConnectionMax;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.connectionmax");
                        _connectionMax = Consts.Consumer_DefaultConnectionMax;
                    }
                }
                ConsumerTraceItems.Instance.ConnectionMax = _connectionMax;
                return _connectionMax;
            }
            private set
            {
                if (value <= 0 || value == _connectionMax) return;
                _connectionMax = value;
                ConsumerTraceItems.Instance.ConnectionMax = _connectionMax;
                NotifyPropertyChange(NotifyProperty.ConnectionMax);
            }
        }

        private int _ackTimeout;
        /// <summary>
        /// ACK超时时间
        /// </summary>
        public int AckTimeout
        {
            get
            {
                if (_ackTimeout < 1)
                {
                    try
                    {
                        int temp;
                        int.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_AckTimeout), out temp);
                        _ackTimeout = temp > 0 ? temp : Consts.Consumer_AckTimeout;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.acktimeout");
                        _ackTimeout = Consts.Consumer_AckTimeout;
                    }
                }
                ConsumerTraceItems.Instance.AckTimeout = _ackTimeout;
                return _ackTimeout;
            }
            private set
            {
                if (value <= 0 || value == _ackTimeout || value > MaxAckTimeout) return;
                _ackTimeout = value;
                ConsumerTraceItems.Instance.AckTimeout = _ackTimeout;
                NotifyPropertyChange(NotifyProperty.AckTimeout);
            }
        }

        private int timeout;
        public int Timeout
        {
            get
            {
                if (timeout < 1)
                {
                    try
                    {
                        int temp;
                        int.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_Timeout), out temp);
                        timeout = temp > 0 ? temp : Consts.Consumer_Timeout;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.acktimeout");
                        timeout = Consts.Consumer_Timeout;
                    }
                }
                ConsumerTraceItems.Instance.Timeout = timeout;
                return timeout;
            }
            private set
            {
                if (value <= 0 || value == timeout) return;
                timeout = value;
                ConsumerTraceItems.Instance.Timeout = timeout;
            }
        }


        private uint _batchSize;
        /// <summary>
        /// 默认取数据数
        /// </summary>
        private uint BatchSize
        {
            get
            {
                if (_batchSize < 1)
                {
                    try
                    {
                        uint temp;
                        uint.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_BatchSize), out temp);
                        _batchSize = temp > 0 ? temp : Consts.Consumer_DefaultBatchSize;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.batchsize");
                        _batchSize = Consts.Consumer_DefaultBatchSize;
                    }
                }
                ConsumerTraceItems.Instance.BatchSize = _batchSize;
                return _batchSize;
            }
            set
            {
                if (value <= 0 || value == _batchSize) return;
                _batchSize = value;
                ConsumerTraceItems.Instance.BatchSize = _batchSize;
                NotifyPropertyChange(NotifyProperty.BatchSize);
            }
        }

        public uint GetBatchSize(string consumer)
        {
            uint batchSize;
            _batchSizeDict.TryGetValue(consumer, out batchSize);
            return batchSize > 0 ? batchSize : BatchSize;
        }

        private uint _receiveTimeout;
        /// <summary>
        /// 
        /// </summary>
        private uint ReceiveTimeout
        {
            get
            {
                if (_receiveTimeout < 1)
                {
                    try
                    {
                        uint temp;
                        uint.TryParse(CentralConfig.GetValue(Consts.Consumer_Config_ReceiveTimeout), out temp);
                        _receiveTimeout = temp > 0 ? temp : Consts.Consumer_DefaultReceiveTimeout;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.configutil.receivetimeout");
                        _receiveTimeout = Consts.Consumer_DefaultReceiveTimeout;
                    }
                }
                ConsumerTraceItems.Instance.ReceiveTimeout = _receiveTimeout;
                return _receiveTimeout;
            }
            set
            {
                if (value <= 0 || value == _receiveTimeout) return;
                _receiveTimeout = value;
                ConsumerTraceItems.Instance.ReceiveTimeout = _receiveTimeout;
                NotifyPropertyChange(NotifyProperty.ReceiveTimeout);
            }
        }

        public uint GetReceiveTimeout(string consumer)
        {
            uint receiveTimeout;
            _receiveTimeoutDict.TryGetValue(consumer, out receiveTimeout);
            return receiveTimeout > 0 ? receiveTimeout : ReceiveTimeout;
        }

        public int GetPoolSize(string consumer){
            int poolSize;
            _poolSizeDict.TryGetValue(consumer,out poolSize);
            return poolSize;
        }
    }

    public enum NotifyProperty
    {
        Capacity,
        ConnectionMax,
        AckTimeout,
        BatchSize,
        ReceiveTimeout,
        PoolSize,
    }
}
