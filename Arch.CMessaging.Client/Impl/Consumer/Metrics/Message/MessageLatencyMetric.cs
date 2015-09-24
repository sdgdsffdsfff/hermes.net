using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cmessaging.consumer.message
{
    internal class MessageLatencyMetric:LatencyMetricBase
    {
        public MessageLatencyMetric()
        {
            //Tags.Add("MessageId", "");
            //Tags.Add("CorrelationID", "");
            Tags.Add("Consumer", "");
        }
        //[Tag]
        //public string MessageId { get { return Tags["MessageId"]; } set { Tags["MessageId"] = value; } }
        //[Tag]
        //public string CorrelationID { get { return Tags["CorrelationID"]; } set { Tags["CorrelationID"] = value; } }
        [Tag]
        public string Consumer { get { return Tags["Consumer"]; } set { Tags["Consumer"] = value; } }


    }
}
