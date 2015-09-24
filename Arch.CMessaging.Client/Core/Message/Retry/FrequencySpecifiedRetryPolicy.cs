using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Arch.CMessaging.Client.Core.Message.Retry
{
    public class FrequencySpecifiedRetryPolicy : IRetryPolicy
    {
        private static Regex PATTERN_VALID = new Regex("\\[(\\d+,*)+\\]");

        private static Regex PATTERN_GROUP = new Regex("(\\d+),*");

        private String policyValue;

        private int retryTimes;

        private List<int> intervals;

        public FrequencySpecifiedRetryPolicy(String policyValue)
        {
            if (policyValue == null)
            {
                throw new Exception("Policy value can not be null");
            }

            policyValue = policyValue.Trim();
            if (PATTERN_VALID.Match(policyValue).Success)
            {
                intervals = new List<int>();
                MatchCollection matches = PATTERN_GROUP.Matches(policyValue.Substring(1, policyValue.Length - 2));
                foreach (Match m in matches)
                {
                    intervals.Add(Convert.ToInt32(m.Groups[1].Value));
                }
                retryTimes = intervals.Count;
            }
            else
            {
                throw new Exception(string.Format("Policy value {0} is invalid", policyValue));
            }
        }

        public int GetRetryTimes()
        {
            return retryTimes;
        }

        public long NextScheduleTimeMillis(int retryTimes, long currentTimeMillis)
        {
            if (retryTimes >= this.retryTimes)
            {
                return 0L;
            }
            else
            {
                return currentTimeMillis + intervals[retryTimes] * 1000;
            }
        }
    }
}

