using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.producer.rcv.nack
{
    public class RcvNackCountMetric : CountMetricBase
    {
        public RcvNackCountMetric()
        {
            Tags.Add("ExchangeName", "");
            Tags.Add("Identifier", "");
            Tags.Add("ServerHostname", "");
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
    }
}
