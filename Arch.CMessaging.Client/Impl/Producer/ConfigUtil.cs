using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Arch.CFramework.Configuration;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using cmessaging.producer.sync;

namespace Arch.CMessaging.Client.Impl.Producer
{
    internal class ConfigUtil
    {
        public delegate void NotifyPropertyChangeDelegate(NotifyProperty property);
        public NotifyPropertyChangeDelegate NotifyPropertyChange;

        private readonly static ConfigUtil _instance = new ConfigUtil();
        public static ConfigUtil Instance
        {
            get { return _instance; }
        }

        private IScheduler SyncMetadataScheduler;
        public void RunScheduler()
        {
            if (SyncMetadataScheduler != null) return;
            SyncMetadataScheduler = ObjectFactory.Current.Get<IScheduler>(Lifetime.ContainerControlled);
            //每隔一分钟刷新一次
            SyncMetadataScheduler.Register(() =>
            {
                int isSuccess = 1;
                try
                {
                    var fxConfigServiceUrl = ConfigurationManager.AppSettings["FxConfigServiceUrl"];
                    if (string.IsNullOrEmpty(fxConfigServiceUrl))
                    {
                        //MetricManagerFactory.MetricManager.Set(new ExceptionCountMetric() { HappenedWhere = ExceptionType.OnMetadataSync.ToString() });
                        return;
                    }

                    CmessagingAdminUrl = CentralConfig.GetValue(Consts.Producer_Config_AdminUrl);
                    ProducerTraceItems.Instance.ProducerConfigUrl = CmessagingAdminUrl;

                    var timeOutStr = CentralConfig.GetValue(Consts.Producer_Config_Timeout);
                    if(string.IsNullOrEmpty(timeOutStr))
                    {
                        Timeout = Consts.Producer_DefaultTimeout;
                    }
                    else
                    {
                        int time;
                        int.TryParse(timeOutStr, out time);
                        Timeout = time;
                    }

                    var connectionMaxCountStr = CentralConfig.GetValue(Consts.Producer_Config_ConnectionMaxCount);
                    if (string.IsNullOrEmpty(connectionMaxCountStr))
                    {
                        ConnectionMaxCount = Consts.Producer_ConnectionMaxCount;
                    }
                    else
                    {
                        int maxCount;
                        int.TryParse(connectionMaxCountStr, out maxCount);
                        ConnectionMaxCount = maxCount;
                    }
                    var connectionCountStr = CentralConfig.GetValue(Consts.Producer_Config_ConnectionCount);
                    if (string.IsNullOrEmpty(connectionCountStr))
                    {
                        connectionCount = -1;
                    }
                    else
                    {
                        int count;
                        int.TryParse(connectionCountStr, out count);
                        ConnectionCount = count;
                    }
                }
                catch (Exception ex)
                {
                    isSuccess = 0;
                    Logg.Write(ex, LogLevel.Error, "cmessaging.producer.configutil.runscheduler");
                }
                finally
                {
                    MetricManagerFactory.MetricManager.Set(new SyncCountMetric { Type = "metadata",IsSuccess = isSuccess.ToString()});
                }
            }, "ConfigScheduler", 60 * 1000, true);
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
                        _cmessagingAdminUrl = CentralConfig.GetValue(Consts.Producer_Config_AdminUrl);
                        ProducerTraceItems.Instance.ProducerConfigUrl = _cmessagingAdminUrl;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.producer.configutil.cmessagingadminurl");
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

        private int timeout;
        public int Timeout
        {
            get
            {
                if(timeout<1)
                {
                    try
                    {
                        var timeoutstr = CentralConfig.GetValue(Consts.Producer_Config_Timeout);
                        int.TryParse(timeoutstr, out timeout);
                        if (timeout < 1) timeout = Consts.Producer_DefaultTimeout;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.producer.configutil.timeout");
                        if (timeout < 1) timeout = Consts.Producer_DefaultTimeout;
                    }
                }
                ProducerTraceItems.Instance.ProducerTimeout = timeout;
                return timeout;
            }
            private set
            {
                if (value <1) return;
                timeout = value;
                ProducerTraceItems.Instance.ProducerTimeout = timeout;
            }
        }

        private int connectionMaxCount = 0;
        public int ConnectionMaxCount
        {
            get
            {
                if (connectionMaxCount < 1)
                {
                    try
                    {
                        var str = CentralConfig.GetValue(Consts.Producer_Config_ConnectionMaxCount);
                        int.TryParse(str, out connectionMaxCount);
                        if (connectionMaxCount < 1) connectionMaxCount = Consts.Producer_ConnectionMaxCount;
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.producer.configutil.connectionmaxcount");
                        if (connectionMaxCount < 1) connectionMaxCount = Consts.Producer_ConnectionMaxCount;
                    }
                }
                ProducerTraceItems.Instance.ConnectionMaxCount = connectionMaxCount;
                return connectionMaxCount;
            }
            set
            {
                if (value < 1) return;
                connectionMaxCount = value;
                ProducerTraceItems.Instance.ConnectionMaxCount = connectionMaxCount;
            }
        }

        private int connectionCount = 0;
        public int ConnectionCount
        {
            get
            {
                if (connectionCount == 0)
                {
                    try
                    {
                        var str = CentralConfig.GetValue(Consts.Producer_Config_ConnectionCount);
                        int.TryParse(str, out connectionCount);
                        if (connectionCount > ConnectionMaxCount) connectionCount = ConnectionMaxCount;//如超最大值则修改为最大值
                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.producer.configutil.connectioncount");
                    }
                    if (connectionCount == 0) connectionCount = Consts.Producer_DefaultConnectionCount;
                }
                ProducerTraceItems.Instance.ConnectionCount = connectionCount;
                return connectionCount;
            }
            set
            {
                if (value < 1 || value > ConnectionMaxCount || connectionCount == value) return;
                connectionCount = value;
                ProducerTraceItems.Instance.ConnectionCount = connectionCount;
                if (NotifyPropertyChange != null)
                    NotifyPropertyChange(NotifyProperty.ConnectionCount);
            }
        }
    }

    public enum  NotifyProperty
    {
        ConnectionCount
    }
}
