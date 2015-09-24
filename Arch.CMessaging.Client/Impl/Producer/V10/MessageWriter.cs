using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class MessageWriter : IMessageWriter
    {
        private PubMessage message;
        private const uint CompressionThreshold = 1024 * 32;
       
        #region IMessageWriter Members
        public void Write(object value, IHeaderProperties header)
        {
            Guard.ArgumentNotNull(value, "value");
            if (!(header is BasicHeader))
                throw new ArgumentException("Only type MessageHeader allowed currently");

            message = new PubMessage();
            byte[] compressedMessage = null;
            header.Serialization = SerializationType.Binary;
            header.ContentEncoding = "utf-8";
            var messageBody = new BinaryTranscoder().Serialize(value, header.Type);
            if (messageBody != null)
                if (header.Compression != CompressionType.None)
                    if (messageBody.Length > CompressionThreshold)
                        compressedMessage = new GzipCompresser().Compress(messageBody);
                    else
                        header.Compression = CompressionType.None;

            message.Body = compressedMessage ?? (messageBody ?? new byte[] { });
            message.Size += message.Body.Length;

            header.ContentLength = message.Body.Length;

            var headerBytes = new ThriftJsonTranscoder().Serialize(header, MessageType.Object);
            if (headerBytes != null && headerBytes.Length > 0)
            {
                message.Header = Encoding.UTF8.GetString(headerBytes);
                message.Size += headerBytes.Length;
            }

            message.ExchangeName = header.ExchangeName;
            message.Subject = header.Subject;
            message.MessageID = header.MessageID;
        }
        #endregion

        public PubMessage ToMessage()
        {
            var tmp = message;
            message = null;
            return tmp;
        }
    }
}
