using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CFramework.AppInternals.Components.HealthCheckComponent;
using cmessaging.producer.exception;
using Arch.CFramework.AppInternals.Components;

namespace Arch.CMessaging.Client.Impl.Producer.Check
{
    public class ProducerHealthCheck : HealthCheckBase
    {
        ProducerExceptionCountMetric exceptionCountMetric = null;
        protected override Result Check()
        {
            try
            {
                if(exceptionCountMetric == null)
                    exceptionCountMetric = (ProducerExceptionCountMetric)ComponentManager.Current.GetComponent(new ProducerExceptionCountMetric().GetType().FullName);
                if (exceptionCountMetric != null)
                {
                    var errorCount = exceptionCountMetric.GetSum();
                    if (errorCount > 0)
                        return Result.UnHealthy("producer client 发送消息异常数" + errorCount);
                }
            }
            catch (Exception ex)
            {
                return Result.UnHealthy(ex.ToString());
            }
            return Result.Healthy("producer client 正常");
        }
    }
}
