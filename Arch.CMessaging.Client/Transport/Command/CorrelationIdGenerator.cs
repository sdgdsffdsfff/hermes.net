using System;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class CorrelationIdGenerator
    {
        public static long generateCorrelationId()
        {
            return new DummyCommand().Header.CorrelationId;
        }

        public class DummyCommand : AbstractCommand
        {

            public DummyCommand()
                : base(CommandType.Dummy, 1)
            {
            }

            private static long serialVersionUID = 4348425282657231872L;

            protected override void ToBytes0(IoBuffer buf)
            {
                throw new Exception("not supported");
            }

            protected override void Parse0(IoBuffer buf)
            {
                throw new Exception("not supported");
            }

        }
    }
}

