using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cmessaging.producer.exception
{
    public class ProducerExceptionCountMetric : CountMetricBase
    {
        public ProducerExceptionCountMetric()
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
