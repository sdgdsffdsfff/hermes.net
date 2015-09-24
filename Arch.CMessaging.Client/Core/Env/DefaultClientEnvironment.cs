using System;
using Freeway.Logging;
using System.Collections.Generic;
using Arch.CMessaging.Client.Core.Utils;
using System.Collections.Concurrent;
using System.Threading;
using System.Configuration;
using System.Collections.Specialized;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Core.Env
{
    [Named(ServiceType = typeof(IClientEnvironment))]
    public class DefaultClientEnvironment : IClientEnvironment, IInitializable
    {
        private static String PRODUCER_DEFAULT_SECTION = "producer";

        private  static String CONSUMER_DEFAULT_SECTION = "consumer";

        private  static String GLOBAL_SECTION = "global";

        private static  String KEY_IS_LOCAL_MODE = "isLocalMode";

        private ConcurrentDictionary<String, Properties> ProducerCache = new ConcurrentDictionary<String, Properties>();

        private ConcurrentDictionary<String, Properties> ConsumerCache = new ConcurrentDictionary<String, Properties>();

        private Properties producerDefault;

        private Properties consumerDefault;

        public Properties GlobalDefault { get; set; }

        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultClientEnvironment));

        private volatile Env env;

        private Dictionary<Env, string> env2MetaDomain = new Dictionary<Env, String>();

        public string getMetaServerDomainName()
        {
            return env2MetaDomain[GetEnv()];
        }

        public Properties GetGlobalConfig()
        {
            return GlobalDefault;
        }

        public Properties GetProducerConfig(String topic)
        {
            // TODO support read topic specific config
            return producerDefault;
        }

        public Properties GetConsumerConfig(String topic)
        {
            // TODO support read topic specific config
            return consumerDefault;
        }

        private Properties readConfigSection(String sectionName)
        {
            return readConfigSection(sectionName, null);
        }

        private Properties readConfigSection(String sectionName, Properties defaults)
        {
            var properties = new Properties();
            NameValueCollection config = ConfigurationManager.GetSection("hermes/" + sectionName) as NameValueCollection;
            if (config != null)
            {
                foreach (string k in config)
                {
                    properties.SetProperty(k, config[k]);
                }
            }

            return properties;
        }

		
        public void Initialize()
        {
            producerDefault = readConfigSection(PRODUCER_DEFAULT_SECTION);
            consumerDefault = readConfigSection(CONSUMER_DEFAULT_SECTION);
            GlobalDefault = readConfigSection(GLOBAL_SECTION);

            Env? resultEnv = Hermes.GetEnv();

            NameValueCollection config = ConfigurationManager.GetSection("hermes/global") as NameValueCollection;
            if (config != null && config["env"] != null)
            {
                Env newEnv = (Env)Enum.Parse(typeof(Env), config["env"].ToUpper());
                if (resultEnv != null && newEnv != resultEnv)
                {
                    throw new Exception(string.Format("Inconsist Hermes env {0} {1}", resultEnv, newEnv));
                }
                else
                {
                    resultEnv = newEnv;
                }
            }

            if (resultEnv == null)
            {
                throw new Exception("Hermes env is not set");
            }

            env = resultEnv.Value;

            env2MetaDomain.Add(Env.LOCAL, GlobalDefault.GetProperty("local.domain", "meta.hermes.local"));
            // TODO use real dev&lpt domain when get dev&lpt domain
            env2MetaDomain.Add(Env.DEV, GlobalDefault.GetProperty("dev.domain", "10.3.8.63"));
            env2MetaDomain.Add(Env.LPT, GlobalDefault.GetProperty("lpt.domain", "10.2.5.133"));
            env2MetaDomain.Add(Env.FAT, GlobalDefault.GetProperty("fat.domain", "meta.hermes.fws.qa.nt.ctripcorp.com"));
            env2MetaDomain.Add(Env.FWS, GlobalDefault.GetProperty("fws.domain", "meta.hermes.fws.qa.nt.ctripcorp.com"));
            env2MetaDomain
                .Add(Env.UAT, GlobalDefault.GetProperty("uat.domain", "meta.hermes.fx.uat.qa.nt.ctripcorp.com"));
            env2MetaDomain.Add(Env.PROD, GlobalDefault.GetProperty("prod.domain", "meta.hermes.fx.ctripcorp.com"));
            env2MetaDomain.Add(Env.PRD, GlobalDefault.GetProperty("prd.domain", "meta.hermes.fx.ctripcorp.com"));

            log.Info(string.Format("Meta server domains: {0}", env2MetaDomain));
        }

        public Env GetEnv()
        {
            return env;
        }

		
        public bool IsLocalMode()
        {
            bool isLocalMode;
            if (GetGlobalConfig().ContainsKey(KEY_IS_LOCAL_MODE))
            {
                isLocalMode = Convert.ToBoolean(GetGlobalConfig().GetProperty(KEY_IS_LOCAL_MODE));
            }
            else
            {
                isLocalMode = false;
            }
            return isLocalMode;
        }

    }
}