using System;

namespace Arch.CMessaging.Client.Core.Message.Payload
{
    public abstract class AbstractPayloadCodec : IPayloadCodec
    {
        public object Decode(byte[] raw, Type type)
        {
            if (type == typeof(RawMessage))
            {
                return new RawMessage(raw);
            }
            else
            {
                return DoDecode(raw, type);
            }
        }

        public byte[] Encode(String topic, Object obj)
        {
            if (obj is RawMessage)
            {
                return ((RawMessage)obj).EncodedMessage;
            }
            else
            {
                return DoEncode(topic, obj);
            }
        }

        public abstract string Type { get; }

        protected abstract byte[] DoEncode(String topic, Object obj);

        protected abstract object DoDecode(byte[] raw, Type type);
    }
}

