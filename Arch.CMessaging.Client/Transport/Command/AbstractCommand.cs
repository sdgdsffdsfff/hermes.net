using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command
{
    public abstract class AbstractCommand : ICommand
    {
        const long serialVersionUID = 1160178108416493829L;

        protected AbstractCommand(CommandType commandType, int version)
        {
            Header = new Header { CommandType = commandType };
            Header.Version = version;
        }

        public Header Header { get; private set; }

        protected IoBuffer Buf { get; set; }

        public void Parse(IoBuffer buf, Header header)
        {
            Header = header;
            Buf = buf;
            Parse0(buf);
        }

        public void ToBytes(IoBuffer buf)
        {
            Header.ToBytes(buf);
            ToBytes0(buf);
        }

        public void Release()
        {
        }

        public void Correlate(ICommand req)
        {
            Header.CorrelationId = req.Header.CorrelationId;
        }

        protected abstract void ToBytes0(IoBuffer buf);

        protected abstract void Parse0(IoBuffer buf);
    }
}
