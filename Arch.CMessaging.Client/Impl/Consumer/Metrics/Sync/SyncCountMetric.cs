using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.sync
{
    //用于记录SERVER请求数
    [MetricScheduling(Circle = 1)]
    public class SyncCountMetric : CountMetricBase
    {
        public SyncCountMetric()
        {
            Tags.Add("StatusCode", "");
            Tags.Add("Type", "");
        }
        [Tag]
        public string StatusCode
        {
            get { return Tags["StatusCode"]; }
            set { Tags["StatusCode"] = value; }
        }
        [Tag]
        public string Type
        {
            get { return Tags["Type"]; }
            set { Tags["Type"] = value; }
        }
    }
}
