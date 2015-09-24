using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer
{
    [MetricScheduling(Circle = 1)]
    public class MemoryMetric:MetricBase
    {
        public MemoryMetric()
        {
            Tags.Add("Consumer", "");
        }
        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }

        [HistogramMetric("memory", Duration = 2, MaxName = "memory.max", MinName = "memory.min", SumName = "memory.sum", AvgName = "memory.avg")]
        public double Value { get; set; }
    }
}
