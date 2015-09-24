using System;
using System.IO;
using System.Text;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    internal class ProducerMessageReader : IMessageReader
    {
        private PubMessage message;
        private BasicHeader messageHeader;
        private Stream bodyStream;

        public string Identifier { get; private set; }

        public ProducerMessageReader(PubMessage message,string identifier)
        {
            this.message = message;
            Identifier = identifier;
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
                        if (!string.IsNullOrEmpty(message.Header))
                        {
                            messageHeader = new ThriftJsonTranscoder().Deserialize<BasicHeader>(Encoding.UTF8.GetBytes(message.Header), MessageType.Object);
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
            if (!HasMessage()) return text;
            var header = this.HeaderProperties;
            if (header != null && message.Body != null)
            {
                if (header.Type == MessageType.Text)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip? new GzipCompresser().Decompress(body): body;
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
            var binary = new byte[0];
            var header = this.HeaderProperties;
            if (!HasMessage()) return binary;
            if (header != null && message.Body != null)
            {
                if (header.Type == MessageType.Binary)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip
                                        ? new GzipCompresser().Decompress(body)
                                        : body;
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
            if (!HasMessage()) return val;
            var header = this.HeaderProperties;
            if (header != null && message.Body != null)
            {
                if (header.Type == MessageType.Object)
                {
                    if (header.ContentLength >= 0 && header.ContentLength <= message.Body.Length)
                    {
                        var body = new byte[header.ContentLength];
                        Array.Copy(message.Body, body, header.ContentLength);

                        var bytes = header.Compression == CompressionType.GZip
                                        ? new GzipCompresser().Decompress(body)
                                        : body;
                        val = new BinaryTranscoder().Deserialize<TObject>(bytes, header.Type);
                    }
                    else
                        throw new IndexOutOfRangeException(
                            "Any Content-Length greater than actual message length considers an invalid value");
                }
            }
            return val;
        }

        public System.IO.Stream GetStream()
        {
            if (!HasMessage()) return bodyStream;
            var header = this.HeaderProperties;
            if (message.Body != null)
            {
                if (header.ContentLength >= 0 && header.ContentLength <= message.Body.Length)
                {
                    var body = new byte[header.ContentLength];
                    Array.Copy(message.Body, body, header.ContentLength);
                    bodyStream = new MemoryStream(body);
                }
                else
                    throw new IndexOutOfRangeException(
                        "Any Content-Length greater than actual message length considers an invalid value");
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
