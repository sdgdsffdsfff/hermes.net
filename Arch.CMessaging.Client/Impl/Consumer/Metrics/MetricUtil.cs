using System;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Client.Impl;

namespace cmessaging.consumer
{
    public static class MetricUtil
    {
        public static void Set(MetricBase metric, double val = 1)
        {
            try
            {
                MetricManagerFactory.MetricManager.Set(metric, val);
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "Consumer.MetricUtil", new[] { new KeyValue() { Key = "MetricName", Value = metric.GetType().Name } });
            }
        }
    }
}
