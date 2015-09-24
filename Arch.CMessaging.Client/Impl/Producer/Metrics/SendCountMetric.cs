using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.producer.send
{
    [MetricScheduling(Circle = 1)]
    public class SendCountMetric:CountMetricBase
    {
        public SendCountMetric()
        {
            Tags.Add("ExchangeName", "");
            Tags.Add("Identifier", "");
            Tags.Add("ClientVersion", "");
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
        public string ClientVersion
        {
            get { return Tags["ClientVersion"]; }
            set { Tags["ClientVersion"] = value; }
        }

        [Tag]
        public string ServerHostname
        {
            get { return Tags["ServerHostname"]; }
            set { Tags["ServerHostname"] = value; }
        }
        
    }
}
