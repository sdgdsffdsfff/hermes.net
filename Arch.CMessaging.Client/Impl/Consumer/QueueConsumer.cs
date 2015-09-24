using System;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Util;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class QueueConsumer : AbstractConsumer, IQueueConsumer
    {
        public QueueConsumer(IConsumerBuffer buffer) : base(buffer) { }

        private string _exchangeName;
        private string _pullingRequestUri;
        private const string Format = "queue://{0}/{1}";

        public void QueueBind(string exchangeName)
        {
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");

            var isChange = false;
            if (_exchangeName != exchangeName)
            {
                _exchangeName = exchangeName;
                isChange = true;
            }

            if (!isChange) return;
            ConfigUtil.Instance.RegisterConsumer(ConfigKey);//添加
            if (!string.IsNullOrEmpty(PullingRequestUri)) ChannelConsumerCountor.RemoveConsumer(PullingRequestUri);//移除CONSUMER记数
            _pullingRequestUri = string.Format(Format, _exchangeName, Identifier);
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
    }
}
