using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Core.Env
{
    public interface IClientEnvironment
    {
        bool IsLocalMode();
        Env GetEnv();
        Properties GetProducerConfig(string topic);
        Properties GetConsumerConfig(string topic);
	    Properties GetGlobalConfig();
        string getMetaServerDomainName();
    }
}