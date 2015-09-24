using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class MessageReader : IMessageReader
    {
        private SubMessage message;
        private BasicHeader messageHeader;
        private Stream bodyStream;
        public MessageReader(SubMessage message)
        {
            this.message = message;
            if (message != null)
                if (this.message.Pub == null)
                    this.message.Pub = new PubMessage();
        }

        #region IMessageReader Members

        public IHeaderProperties HeaderProperties
        {
            get
            {
                if (HasMessage())
                {
                    if (messageHeader == null)
                    {
                        if (!string.IsNullOrEmpty(message.Pub.Header))
                        {
                            messageHeader = new ThriftJsonTranscoder().Deserialize<BasicHeader>(
                                Encoding.UTF8.GetBytes(message.Pub.Header), MessageType.Object);
                        }
                    }
                }
                return messageHeader;
            }
        }

        public bool HasMessage()
        {
            return message != null;
        }

        public string GetText()
        {
            string text = string.Empty;
            var header = this.HeaderProperties;
            if (header != null && message.Pub.Body != null)
            {
                if (header.Type == MessageType.Text)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Pub.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Pub.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip
                            ? new GzipCompresser().Decompress(body) : body;
                        text = new BinaryTranscoder().Deserialize<string>(bytes, header.Type);
                    }
                    else
                        throw new IndexOutOfRangeException("Any Content-Length greater than actual message length considers an invalid value");
                }
            }
            return text;
        }

        public byte[] GetBinary()
        {
            byte[] binary = new byte[0];
            var header = this.HeaderProperties;
            if (header != null && message.Pub.Body != null)
            {
                if (header.Type == MessageType.Binary)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Pub.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Pub.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip
                            ? new GzipCompresser().Decompress(body) : body;
                        binary = new BinaryTranscoder().Deserialize<byte[]>(bytes, header.Type);
                    }
                    else
                        throw new IndexOutOfRangeException("Any Content-Length greater than actual message length considers an invalid value");
                }
            }
            return binary;
        }

        public TObject GetObject<TObject>()
        {
            TObject val = default(TObject);
            var header = this.HeaderProperties;
            if (header != null && message.Pub.Body != null)
            {
                if (header.Type == MessageType.Object)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Pub.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Pub.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip
                            ? new GzipCompresser().Decompress(body) : body;
                        val = new BinaryTranscoder().Deserialize<TObject>(bytes, header.Type);
                    }
                    else
                        throw new IndexOutOfRangeException("Any Content-Length greater than actual message length considers an invalid value");
                }
            }
            return val;
        }

        public System.IO.Stream GetStream()
        {
            var header = this.HeaderProperties;
            if (message.Pub.Body != null)
            {
                if (header.ContentLength >= 0 && header.ContentLength <= message.Pub.Body.Length)
                {
                    var body = new byte[header.ContentLength];
                    Array.Copy(message.Pub.Body, body, header.ContentLength);
                    bodyStream = new MemoryStream(body);
                }
                else
                    throw new IndexOutOfRangeException("Any Content-Length greater than actual message length considers an invalid value");
            }
            return bodyStream;
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (bodyStream != null)
                bodyStream.Dispose();
        }

        #endregion
    }
}
