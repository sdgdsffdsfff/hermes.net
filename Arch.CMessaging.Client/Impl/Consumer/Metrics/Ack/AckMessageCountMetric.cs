using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.ack.message
{
    [MetricScheduling(Circle = 1)]
    public class AckMessageCountMetric : CountMetricBase
    {
        public AckMessageCountMetric()
        {
            Tags.Add("Consumer", "");
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }
    }
}
