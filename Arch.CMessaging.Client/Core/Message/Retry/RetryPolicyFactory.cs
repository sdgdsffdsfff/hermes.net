using System;
using System.Collections.Concurrent;

namespace Arch.CMessaging.Client.Core.Message.Retry
{
    public class RetryPolicyFactory
    {

        private static ConcurrentDictionary<string, IRetryPolicy> cache = new ConcurrentDictionary<string, IRetryPolicy>();

        public static IRetryPolicy Create(string policyValue)
        {
            if (policyValue != null)
            {
                IRetryPolicy policy = null;
                cache.TryGetValue(policyValue, out policy);
                if (policy == null)
                {
                    policy = CreatePolicy(policyValue);
                    cache.TryAdd(policyValue, policy);
                }

                if (policy != null)
                {
                    return policy;
                }
            }

            throw new Exception(String.Format("Unknown retry policy for value {0}", policyValue));
        }

        private static IRetryPolicy CreatePolicy(string policyValue)
        {
            if (policyValue.IndexOf(":") != -1)
            {
                string[] splits = policyValue.Split(new string[]{ ":" }, StringSplitOptions.None);
                if (splits != null && splits.Length == 2)
                {
                    string type = splits[0];
                    string value = splits[1];

                    if (type != null && !"".Equals(type.Trim()) && value != null && !"".Equals(value.Trim()))
                    {

                        switch (type.Trim())
                        {
                            case "1":
                                return new FrequencySpecifiedRetryPolicy(value.Trim());

                            case "2": 
                                if ("[]".Equals(value.Trim()))
                                {
                                    return new NoRetryPolicy();
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            throw new Exception(string.Format("Unknown retry policy for value {0}", policyValue));
        }
    }
}

