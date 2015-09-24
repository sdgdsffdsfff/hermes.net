using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.pulling.response
{
    [MetricScheduling(Circle = 1)]
    public class PullingResponseCountMetric : CountMetricBase
    {
        public PullingResponseCountMetric()
        {
            Tags.Add("ServerHostName", "");
            Tags.Add("Consumer", "");
            Tags.Add("StatusCode", "");
            Tags.Add("LatencyDistribution", "");
            Tags.Add("HasMessages", "");
            Tags.Add("Error", "");
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

        [Tag]
        public string StatusCode
        {
            get { return Tags["StatusCode"]; }
            set { Tags["StatusCode"] = value; }
        }

        [Tag]
        public string Error
        {
            get { return Tags["Error"]; }
            set { Tags["Error"] = value; }
        }

        [Tag]
        public string HasMessages
        {
            get { return Tags["HasMessages"]; }
            set { Tags["HasMessages"] = value; }
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
