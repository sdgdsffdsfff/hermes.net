using Arch.CFramework.AppInternals.Components;
using Arch.CMessaging.Client.Impl.Consumer.Log;
using Arch.CMessaging.Client.Impl.Validate;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Core.Util;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Consumer
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
#if DEBUG
                var directory = AppDomain.CurrentDomain.BaseDirectory;

                using (var sw = System.IO.File.AppendText(Path.Combine(directory, @"list.txt")))
                {
                    sw.WriteLine("-------------------------------------------------------------------------------{0}",
                                 DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"));
                    //for (int i = 0; i < consumerStrs.Length; i++)
                    //{
                    //    sw.WriteLine("{0}------>{1}", consumerStrs[i],
                    //                 new HexStringConverter().ToString(
                    //                     MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(consumerStrs[i]))));
                    //}
                }
#endif
                _factory = ObjectFactory.Current;
                _factory.LifetimeManager.Register(Consts.TenMinutesLasting, Lifetime.Lasting,
                                                  new LastingLifetime(10*60*1000));
                var typetypeMapping = new TypeTypeMapping(_factory)
                    .Register<IScheduler, TimerScheduler>(Lifetime.ContainerControlled)
                    .Register<ILog, CLogging>(Lifetime.ContainerControlled)
                    .Register<ITimeProvider, DefaultTimeProvider>(Lifetime.ContainerControlled)
                    .Register<IMetricManager, MetricManager>(Lifetime.ContainerControlled);
                ComponentManager.Current.Register(new ConsumerExceptionHealthCheck());
#if DEBUG
                typetypeMapping.Register<IDebugLogWriter>(
                    new FileLogWriter(directory, new LogPartitionerByConsumer(new string[0])),
                    Lifetime.ContainerControlled, null);
#endif
            }
        }
    }
}
