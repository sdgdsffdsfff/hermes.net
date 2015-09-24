using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class RemoteServerReader
    {
        private static RemoteServerReader serverReader = new RemoteServerReader();

        public static RemoteServerReader GetInstance()
        {
            return serverReader;
        }

        private RemoteServerReader()
        {
        }

        //string uri = "http://cmessaging.arch.sh.ctripcorp.com/CmessagingWebSit/Service/CMessageConfigServer.asmx";
        public string[] GetCollectorService(string uri, string producers)
        {
            var serverList = new List<string>();
            try
            {
                WebRequest request = WebRequest.Create(uri + "/GetPhysicalServerList");
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Headers.Add("producers", producers);
                request.Headers.Add("clientip",Local.IPV4);
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream, System.Text.Encoding.UTF8);
                string json = reader.ReadToEnd();
                json = json.Replace(
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<string xmlns=\"http://tempuri.org/\">", "")
                    .Replace("</string>", "");
                var objList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JServer>>(json);

                foreach (var entity in objList)
                {
                    if (entity.Type == 1)
                    {
                        serverList.Add(entity.ServerDNS + "/collect?Action=Send");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Access service " + uri + "/GetPhysicalServerList failed.", ex);
            }

            return serverList.ToArray<string>();
        }

        public List<ExchangePhysicalServer> GetCollects(string exchanges)
        {
            var cmessagingAdminUrl = ConfigUtil.Instance.CmessagingAdminUrl;
            if (cmessagingAdminUrl != null) cmessagingAdminUrl = cmessagingAdminUrl.Trim();
            if (string.IsNullOrEmpty(cmessagingAdminUrl))
            {
                throw new ConfigurationErrorsException(string.Format("'{0}' setting is not exists or not value.", Consts.Producer_Config_AdminUrl));
            }
            //cmessagingAdminUrl = cmessagingAdminUrl.EndsWith("/")
            //                         ? string.Format("{0}{1}", cmessagingAdminUrl, exchanges)
            //                         : string.Format("{0}/{1}", cmessagingAdminUrl, exchanges);
            //var pageData = wc.DownloadData(cmessagingAdminUrl); get
            ProducerTraceItems.Instance.AdminUrl = cmessagingAdminUrl;
            try
            {
                var wc = new WebClient { Credentials = CredentialCache.DefaultCredentials };
                wc.Headers.Add(HttpRequestHeader.Accept, "json");
                var enc = Encoding.Default;

                var nameValueCollection = new NameValueCollection {{"exchanges", exchanges}};
                var pageData = wc.UploadValues(cmessagingAdminUrl, "POST", nameValueCollection);

                var str = enc.GetString(pageData);
                //var serializer = new JavaScriptSerializer();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ExchangePhysicalServer>>(str);
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "cmessaging.prodcuer.remoteserverreader.getcollects",
                    new[]
                            {
                                new KeyValue{ Key="adminsvcurl",Value=cmessagingAdminUrl},
                                new KeyValue{ Key="exchanges",Value=exchanges},
                            });
                throw ex;
            }
        }

        public string[] GetDispatcherService(string uri)
        {
            List<string> serverList = new List<string>();
            try
            {
                WebRequest request = WebRequest.Create(uri + "/GetPhysicalServerList");
                request.Credentials = CredentialCache.DefaultCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream, System.Text.Encoding.UTF8);
                string json = reader.ReadToEnd();
                json = json.Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<string xmlns=\"http://tempuri.org/\">", "")
                    .Replace("</string>", "");
                var objList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JServer>>(json);

                foreach (var entity in objList)
                {
                    if (entity.Type == 2)
                    {
                        serverList.Add(entity.ServerDNS + "/dispatch");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Access service " + uri + "/GetPhysicalServerList failed.", ex);
            }

            return serverList.ToArray<string>();
        }
    }

    public class JServer
    {
        public int ServerId { get; set; }

        public string ServerName { get; set; }

        public string ServerIP { get; set; }

        public string ServerDNS { get; set; }

        public int Type { get; set; }

        public string TypeName { get; set; }

        public int Weight { get; set; }

        public DateTime CreateTime { get; set; }

    }
}
