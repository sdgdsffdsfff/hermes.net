using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.exception
{
    [MetricScheduling(Circle = 1)]
    public class ExceptionCountMetric:CountMetricBase
    {
        public ExceptionCountMetric()
        {
            Tags.Add("Consumer", "");
            Tags.Add("HappenedWhere", "");
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }
        [Tag]
        public string HappenedWhere
        {
            get { return Tags["HappenedWhere"]; }
            set { Tags["HappenedWhere"] = value; }
        }
    }
}
