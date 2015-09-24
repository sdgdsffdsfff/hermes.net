using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.pulling.response
{
    [MetricScheduling(Circle = 1)]
    public class PullingResponseLatencyMetric:LatencyMetricBase
    {
        public PullingResponseLatencyMetric()
        {
            Tags.Add("Consumer", "");
            Tags.Add("ServerHostName", "");
        }
        [Tag]
        public string ServerHostName
	    {
            get { return Tags["ServerHostName"]; }
            set { Tags["ServerHostName"] = value; }
	    }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }
    }
}
