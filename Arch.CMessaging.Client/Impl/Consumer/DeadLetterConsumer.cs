using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Util;
using System.IO;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class DeadLetterConsumer : AbstractConsumer,IDeadLetterConsumer
    {
        private string _topic;
        private string _exchangeName;
        private string _pullingRequestUri;
        private const string Format = "dead:{0}//{1}/{2}";

        public DeadLetterConsumer(IConsumerBuffer buffer) : base(buffer) { }

        public void DeadLetterBind(string exchangeName, string topic = null)
        {
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            

            var isChange = false;
            if (_topic != topic)
            {
                _topic = topic;
                if (!string.IsNullOrEmpty(_topic))
                    checkTopic(_topic);
                isChange = true;
            }
            if (_exchangeName != exchangeName)
            {
                _exchangeName = exchangeName;
                isChange = true;
            }

            if (!isChange) return;
            ConfigUtil.Instance.RegisterConsumer(ConfigKey);//添加
            if (!string.IsNullOrEmpty(PullingRequestUri)) ChannelConsumerCountor.RemoveConsumer(PullingRequestUri);//移除CONSUMER记数
            _pullingRequestUri = string.Format(Format, _topic, _exchangeName, Identifier);
            Buffer.PullingRequestUri = _pullingRequestUri;
            Buffer.BatchSize = (int)this.BatchSize;
            Buffer.ReceiveTimeout = (int)this.ReceiveTimeout;
            ChannelConsumerCountor.AddConsumer(PullingRequestUri);//添加CONSUMER记数

#if DEBUG
                var directory = AppDomain.CurrentDomain.BaseDirectory;
                using (var sw = System.IO.File.AppendText(Path.Combine(directory, @"list.txt")))
                {
                    sw.WriteLine("{0}------>{1}", PullingRequestUri,
                                 new HexStringConverter().ToString(
                                     MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(PullingRequestUri))));
                }
#endif
        }


        protected override string PullingRequestUri
        {
            get { return _pullingRequestUri; }
        }
        protected override string ExchangeName
        {
            get { return _exchangeName; }
        }

        private void checkTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return;
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new Exception("Topic不能为空.");
            }
            if (topic.Length > 80)
            {
                throw new Exception("Topic长度不能超过80字符.");
            }
            var topics = topic.Split(',');
            if (topics.Length > 5)
            {
                throw new Exception("同时最多支持订阅五个Topic");
            }
            var r = new Regex("^[.#*,a-zA-Z0-9]+$");
            var m = r.Match(topic);
            if (!m.Success)
            {
                throw new Exception("Topic只能使用数字，大写和小写英文字母，点号，星号，井号.");
            }
        }
    }
}
