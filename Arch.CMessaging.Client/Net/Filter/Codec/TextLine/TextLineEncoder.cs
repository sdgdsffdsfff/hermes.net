﻿using System;
using System.Text;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Net.Filter.Codec.TextLine
{
    public class TextLineEncoder : IProtocolEncoder
    {
        private readonly Encoding _encoding;
        private readonly LineDelimiter _delimiter;
        private Int32 _maxLineLength = Int32.MaxValue;

        public TextLineEncoder()
            : this(LineDelimiter.Unix)
        { }

        public TextLineEncoder(String delimiter)
            : this(new LineDelimiter(delimiter))
        { }

        public TextLineEncoder(LineDelimiter delimiter)
            : this(Encoding.Default, delimiter)
        { }

        public TextLineEncoder(Encoding encoding)
            : this(encoding, LineDelimiter.Unix)
        { }

        public TextLineEncoder(Encoding encoding, String delimiter)
            : this(encoding, new LineDelimiter(delimiter))
        { }

        public TextLineEncoder(Encoding encoding, LineDelimiter delimiter)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (delimiter == null)
                throw new ArgumentNullException("delimiter");
            if (LineDelimiter.Auto.Equals(delimiter))
                throw new ArgumentException("AUTO delimiter is not allowed for encoder.");

            _encoding = encoding;
            _delimiter = delimiter;
        }

        public Int32 MaxLineLength
        {
            get { return _maxLineLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("maxLineLength (" + value + ") should be a positive value");
                _maxLineLength = value;
            }
        }

        
        public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            String value = message == null ? String.Empty : message.ToString();
            value += _delimiter.Value;
            Byte[] bytes = _encoding.GetBytes(value);
            if (bytes.Length > _maxLineLength)
                throw new ArgumentException("Line too long: " + bytes.Length);

            // TODO BufferAllocator
            IoBuffer buf = IoBuffer.Wrap(bytes);
            output.Write(buf);
        }

        
        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
