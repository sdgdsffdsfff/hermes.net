namespace Arch.CMessaging.Client.Core.Message.Payload
{
    public class RawMessage
    {
        
        public byte[] EncodedMessage { get; private set; }

        public RawMessage(byte[] ecodedMessage)
        {
            EncodedMessage = ecodedMessage;
        }

    }
}

