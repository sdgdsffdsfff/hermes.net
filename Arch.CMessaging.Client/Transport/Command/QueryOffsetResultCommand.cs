using System;
using Arch.CMessaging.Client.Core.Bo;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class QueryOffsetResultCommand : AbstractCommand
    {
        public Offset Offset { get; private set; }

        public QueryOffsetResultCommand()
            : this(null)
        {
        }

        public QueryOffsetResultCommand(Offset offset)
            : base(CommandType.ResultQueryOffset, 1)
        {
            Offset = offset;
        }

        protected override void Parse0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            Offset = codec.ReadOffset();
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            codec.WriteOffset(Offset);
        }
    }
}

