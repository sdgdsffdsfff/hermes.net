using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.pulling.discard
{
    /// <summary>
    /// PULLING消息丢弃数
    /// </summary>
    [MetricScheduling(Circle = 1)]
    public class PullingDiscardCountMetric : CountMetricBase
    {
        public PullingDiscardCountMetric()
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
