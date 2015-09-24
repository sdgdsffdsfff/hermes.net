using Arch.CFramework.AppInternals.Components;
using Arch.CMessaging.Client.Impl.Producer.Check;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Time;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class ObjectFactoryLifetimeManager
    {
        private static readonly ObjectFactoryLifetimeManager Manager = new ObjectFactoryLifetimeManager();
        private static ObjectFactory _factory = null;
        private static readonly object O = new object();
        public static ObjectFactoryLifetimeManager Instance
        {
            get { return Manager; }
        }

        public void Register()
        {
            //if (_factory != null) return;
            lock (O)
            {
                if (_factory != null) return;
                _factory = ObjectFactory.Current;
                _factory.LifetimeManager.Register(Consts.TenMinutesLasting, Lifetime.Lasting,
                                                  new LastingLifetime(10 * 60 * 1000));
                var typetypeMapping = new TypeTypeMapping(_factory)
                    .Register<IScheduler, TimerScheduler>(Lifetime.ContainerControlled)
                    .Register<ILog, CLogging>(Lifetime.ContainerControlled)
                    .Register<ITimeProvider, DefaultTimeProvider>(Lifetime.ContainerControlled)
                    .Register<IMetricManager, MetricManager>(Lifetime.ContainerControlled);
                ComponentManager.Current.Register(new ProducerHealthCheck());
            }
        }
    }
}
