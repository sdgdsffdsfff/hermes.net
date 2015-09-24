using System;
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
    public class TopicConsumer : AbstractConsumer, ITopicConsumer
    {
        private static ThreadSafe.Integer _topicCount = new ThreadSafe.Integer(0);

        private string _topic;
        private string _exchangeName;
        private string _queueName;
        private string _pullingRequestUri;
        private const string Format = "topic:{0}//{1}/{2}{3}{4}";
        

        public TopicConsumer(IConsumerBuffer buffer) : base(buffer) { }

        public string HeaderFilter
        {
            get { return null; }
        }

        public void TopicBind(string topic, string exchangeName, string queueName = null)
        {
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");
            Guard.ArgumentNotNullOrEmpty(topic, "topic");
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");

            var isChange = false;
            if (_topic != topic)
            {
                checkTopic(topic);
                _topic = topic;
                isChange = true;
            }
            if (_exchangeName != exchangeName)
            {
                _exchangeName = exchangeName;
                isChange = true;
            }
            if (_queueName != queueName)
            {
                _queueName = queueName;
                isChange = true;
            }
            if(string.IsNullOrEmpty(_queueName))
            {
                _queueName = new HexStringConverter()
                    .ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(topic + exchangeName + Identifier)));
            }

            if (!isChange) return;
            ConfigUtil.Instance.RegisterConsumer(ConfigKey);//添加
            if (!string.IsNullOrEmpty(PullingRequestUri)) ChannelConsumerCountor.RemoveConsumer(PullingRequestUri);//移除CONSUMER记数
            _pullingRequestUri = string.Format(Format, _topic, _exchangeName, Identifier,
                                               string.IsNullOrEmpty(_queueName) ? "" : "/" + _queueName,
                                               string.IsNullOrEmpty(HeaderFilter) ? "" : "?filter=" + HeaderFilter);
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

            var currentTopicCount = string.IsNullOrEmpty(_topic) ? 0 : _topic.Split(',').Length;
            //TOPIC数据检测
            var count = _topicCount.AtomicAddAndGet(topics.Length - currentTopicCount);
            var topicCount = ConfigUtil.Instance.TopicCount;
            if (count > topicCount)
                throw new Exception(string.Format(string.Format("Topic数最大为{0},现已超出此数!", topicCount)));
        }
    }
}
