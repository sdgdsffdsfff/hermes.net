using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.pulling.message
{
    [MetricScheduling(Circle = 1)]
    public class PullingMessageCountMetric:CountMetricBase
    {
        public PullingMessageCountMetric()
        {
            Tags.Add("Consumer", "");
            Tags.Add("MessageSizeDistribution","");
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }

        public int MessageSize
        {
            set { MessageSizeDistribution = Util.GetMessageSizeDistribution(value); }
        }

        /// <summary>
        /// 不允许直接设值，请通过设置MessageSize
        /// </summary>
        [Tag]
        public string MessageSizeDistribution
        {
            get { return Tags["MessageSizeDistribution"]; }
            set { Tags["MessageSizeDistribution"] = value; }
        }
    }
}
