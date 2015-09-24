using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.handling
{
    [MetricScheduling(Circle = 1)]
    public class HandlingCountMetric:CountMetricBase
    {
        public HandlingCountMetric()
        {
            Tags.Add("Consumer", "");
            Tags.Add("LatencyDistribution", "");
            Tags.Add("MessageLatencyDistribution", "");
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }

        public double Latency
        {
            set { LatencyDistribution = Util.GetLatencyDistribution(value); }
        }
        /// <summary>
        /// 不允许直接设值，请通过设置Latency
        /// </summary>
        [Tag]
        public string LatencyDistribution
        {
            get { return Tags["LatencyDistribution"]; }
            set { Tags["LatencyDistribution"] = value; }
        }

        public double MessageLatency { set { MessageLatencyDistribution = Util.GetMessageLatencyDistribution(value); } }
         [Tag]
        public string MessageLatencyDistribution
        {
            get { return Tags["MessageLatencyDistribution"]; }
            set { Tags["MessageLatencyDistribution"] = value; }
        }
    }
}
