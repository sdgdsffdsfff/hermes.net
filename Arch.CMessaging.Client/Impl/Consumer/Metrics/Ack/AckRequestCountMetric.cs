using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.ack.request
{
    [MetricScheduling(Circle = 1)]
    public class AckRequestCountMetric:CountMetricBase
    {
        public AckRequestCountMetric()
        {
            Tags.Add("ServerHostName", "");
        }

        [Tag]
        public string ServerHostName
        {
            get { return Tags["ServerHostName"]; }
            set { Tags["ServerHostName"] = value; }
        }
    }
}
