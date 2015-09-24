using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer
{
    //用于记录生成Channel数
    [MetricScheduling(Circle = 1)]
    public class ConsumerCountMetric : CountMetricBase
    {
        public ConsumerCountMetric()
        {
            Tags.Add("consumer", "");
        }
        [Tag]
        public string consumer { get { return Tags["consumer"]; } set { Tags["consumer"] = value; } }
    }
}
