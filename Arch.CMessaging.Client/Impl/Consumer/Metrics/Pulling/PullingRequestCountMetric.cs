using Arch.CFramework.AppInternals.Components.MetricComponents.Attributes;
using Arch.CMessaging.Client.Impl;
using Arch.CMessaging.Core.CFXMetrics;

namespace cmessaging.consumer.pulling.request
{
    [MetricScheduling(Circle = 1)]
    public class PullingRequestCountMetric : CountMetricBase
    {
        public PullingRequestCountMetric()
        {
            Tags.Add("Consumer", "");
            Tags.Add("ServerHostName", "");
            Tags.Add("ClientVersion", Version.AssemblyVersion);
        }
        [Tag]
        public string ServerHostName
        {
            get { return Tags["ServerHostName"]; }
            set { Tags["ServerHostName"] = value; }
        }

        [Tag]
        public string Consumer
        {
            get { return Tags["Consumer"]; }
            set { Tags["Consumer"] = value; }
        }

        [Tag]
        public string ClientVersion
        {
            get { return Tags["ClientVersion"]; }
            set { Tags["ClientVersion"] = Version.AssemblyVersion; }
        }
    }
}
