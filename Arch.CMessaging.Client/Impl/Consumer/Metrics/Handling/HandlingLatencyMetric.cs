using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.handling
{
    [MetricScheduling(Circle = 1)]
    public class HandlingLatencyMetric:LatencyMetricBase
    {
        public HandlingLatencyMetric()
        {
            Tags.Add("Consumer", "");
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }
    }
}
