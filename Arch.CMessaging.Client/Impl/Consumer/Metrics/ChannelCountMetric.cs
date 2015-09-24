using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.channel
{
    //用于记录生成Channel数
    [MetricScheduling(Circle = 1)]
    public class ChannelCountMetric : CountMetricBase
    {
        public ChannelCountMetric()
        {
        }
    }
}
