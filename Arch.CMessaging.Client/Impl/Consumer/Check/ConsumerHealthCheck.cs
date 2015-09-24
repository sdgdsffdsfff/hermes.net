using System;
using Arch.CFramework.AppInternals.Components;
using Arch.CFramework.AppInternals.Components.HealthCheckComponent;
using cmessaging.consumer.exception;
using cmessaging.consumer.noserver;

namespace Arch.CMessaging.Client.Impl.Validate
{
    public class ConsumerExceptionHealthCheck : HealthCheckBase
    {
        ExceptionCountMetric metric = null;
        NoServerCountMetric noserverMetric = null;
        protected override Result Check()
        {
            try
            {
                if (noserverMetric == null)
                {
                    noserverMetric = (NoServerCountMetric)ComponentManager.Current.GetComponent(new NoServerCountMetric().GetType().FullName);
                }
                if (noserverMetric != null)
                {
                    var count = noserverMetric.GetSum();
                    if (count > 0 && noserverMetric.Metrics != null && noserverMetric.Metrics.Count>0)
                    {
                        string str = "";
                        foreach (var item in noserverMetric.Metrics)
                        {
                            str += item.Tags["consumer"] + ",";
                        }
                        return Result.UnHealthy(string.Format("consumer[{0}]未找到服务器", str));
                    }
                }

                if (metric == null)
                {
                    metric = (ExceptionCountMetric)ComponentManager.Current.GetComponent(new ExceptionCountMetric().GetType().FullName);
                }
                if (metric != null)
                {
                    var errorCount = metric.GetSum();
                    if(errorCount>0)
                        return Result.UnHealthy("consumer client 异常数" + errorCount);
                }
            }
            catch (Exception ex)
            {
                return Result.UnHealthy(ex.ToString());
            }
            return Result.Healthy("consumer client 正常");
        }
    }
}
