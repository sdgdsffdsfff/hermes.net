using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;
using cmessaging.consumer;

namespace cmessaging.producer.send.response
{
    public class SendResponseCountMetric : CountMetricBase
    {
        public SendResponseCountMetric()
        {
            Tags.Add("ExchangeName", "");
            Tags.Add("Identifier", "");
            Tags.Add("error", "");
            Tags.Add("ServerHostname", "");
            Tags.Add("LatencyDistribution", "");
            Tags.Add("StatusCode", "");
        }
        [Tag]
        public string ExchangeName
        {
            get { return Tags["ExchangeName"]; }
            set { Tags["ExchangeName"] = value; }
        }

        [Tag]
        public string Identifier
        {
            get { return Tags["Identifier"]; }
            set { Tags["Identifier"] = value; }
        }

        [Tag]
        public string ServerHostname
        {
            get { return Tags["ServerHostname"]; }
            set { Tags["ServerHostname"] = value; }
        }
        [Tag]
        public string error
        {
            get { return Tags["error"]; }
            set { Tags["error"] = value; }
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
