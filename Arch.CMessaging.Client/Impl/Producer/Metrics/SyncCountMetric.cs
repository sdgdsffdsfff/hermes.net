using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.producer.sync
{
    //用于记录SERVER请求数
    [MetricScheduling(Circle = 1)]
    public class SyncCountMetric : CountMetricBase
    {
        public SyncCountMetric()
        {
            Tags.Add("Type", "");
            Tags.Add("IsSuccess", "");
        }

        [Tag]
        public string Type
        {
            get { return Tags["Type"]; }
            set { Tags["Type"] = value; }
        }

        [Tag]
        public string IsSuccess
        {
            get { return Tags["IsSuccess"]; }
            set { Tags["IsSuccess"] = value; }
        }
    }
}
