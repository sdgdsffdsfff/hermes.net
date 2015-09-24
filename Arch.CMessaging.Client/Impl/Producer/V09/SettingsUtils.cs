using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class SettingsUtils
    {
        private static readonly IDictionary<string, string> settingsDic = new Dictionary<string, string>();

        static SettingsUtils()
        {
            InitAppSettings();
        }

        private static void InitAppSettings()
        {
            NameValueCollection settings = ConfigurationManager.AppSettings;
            string[] configKeys = ConfigurationManager.AppSettings.AllKeys;

            foreach (string key in configKeys)
            {
                settingsDic[key] = ConfigurationManager.AppSettings[key];
            }
        }

        public static IDictionary<string, string> GetAppSettings()
        {
            return settingsDic;
        }
    }
}
