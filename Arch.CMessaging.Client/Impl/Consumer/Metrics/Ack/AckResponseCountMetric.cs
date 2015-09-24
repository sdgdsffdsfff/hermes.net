using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.ack.response
{
    [MetricScheduling(Circle = 1)]
    public class AckResponseCountMetric:CountMetricBase
    {
        public AckResponseCountMetric()
        {
            Tags.Add("ServerHostName", "");
            Tags.Add("StatusCode", "");
            Tags.Add("LatencyDistribution", "");
        }

        [Tag]
        public string ServerHostName
        {
            get { return Tags["ServerHostName"]; }
            set { Tags["ServerHostName"] = value; }
        }
        [Tag]
        public string StatusCode
        {
            get { return Tags["StatusCode"]; }
            set { Tags["StatusCode"] = value; }
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
    }
}
