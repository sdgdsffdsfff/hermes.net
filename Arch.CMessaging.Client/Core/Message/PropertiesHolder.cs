﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch.CMessaging.Client.Core.Message
{
    public class PropertiesHolder
    {
        private const String SYS = "SYS.";
        private const string APP = "APP.";
        private Dictionary<string, string> durableProperties;
        private Dictionary<string, string> volatileProperties;
        private HashSet<string> rawDurableAppPropertyNames;
        public PropertiesHolder()
        {
            this.rawDurableAppPropertyNames = new HashSet<string>();
            durableProperties = new Dictionary<string, string>();
            volatileProperties = new Dictionary<string, string>();
        }
        public Dictionary<string, string> DurableProperties 
        {
            get { return durableProperties; }
            set
            {
                if (value != null)
                {
                    durableProperties = value;
                    foreach(var mergedKey in durableProperties.Keys)
                    {
                        if (mergedKey.StartsWith(APP)) 
                        {
                            var sub = mergedKey.Substring(APP.Length);
                            if (!rawDurableAppPropertyNames.Contains(sub))
                                rawDurableAppPropertyNames.Add(sub);
                        }
                    }
                }
            }
        }

        public Dictionary<string, string> VolatileProperties
        {
            get { return volatileProperties; }
            set
            {
                if (value != null) volatileProperties = value;
            }
        }

        public string GetDurableAppProperty(string name)
        {
            return DurableProperties[APP + name];
        }

        public void AddDurableAppProperty(string name, string value)
        {
            if (!rawDurableAppPropertyNames.Contains(name))
                rawDurableAppPropertyNames.Add(name);
            DurableProperties[APP + name] = value;
        }

        public string GetDurableSysProperty(string name)
        {
            return DurableProperties[SYS + name];
        }

        public void AddDurableSysProperty(string name, string value)
        {
            DurableProperties[SYS + name] = value;
        }

        public void AddVolatileProperty(string name, string value)
        {
            VolatileProperties[name] = value;
        }

        public string GetVolatileProperty(string name)
        {
            return VolatileProperties[name];
        }
    }
}