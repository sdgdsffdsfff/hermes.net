using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.noserver
{
    //用于记录Consumer没有取到服务器
    [MetricScheduling(Circle = 1)]
    public class NoServerCountMetric : CountMetricBase
    {
        public NoServerCountMetric()
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
