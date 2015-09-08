using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class SendMessageAckCommand : AbstractCommand
    {
        private const long serialVersionUID = -2462726426306841225L;

        public SendMessageAckCommand()
            : base(CommandType.AckMessageSend, 1)
        {
        }

        public bool Success { get; set; }

        protected override void ToBytes0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            codec.WriteBoolean(Success);
        }

        protected override void Parse0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            Success = codec.ReadBoolean();
        }
    }
}
