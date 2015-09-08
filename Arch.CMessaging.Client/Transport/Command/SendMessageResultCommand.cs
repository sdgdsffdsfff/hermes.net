using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class SendMessageResultCommand : AbstractCommand
    {
        private int totalSize;
        private const long serialVersionUID = -2408812182538982540L;
        private Dictionary<int, bool> successes;

        public SendMessageResultCommand()
            : this(0)
        {
        }

        public SendMessageResultCommand(int totalSize)
            : base(CommandType.ResultMessageSend, 1)
        {
            this.totalSize = totalSize;
            this.successes = new Dictionary<int, bool>();
        }

        public bool IsAllResultSet()
        {
            return successes.Count == totalSize;
        }

        public void AddResults(Dictionary<int, bool> results)
        {
            if (results != null)
            {
                foreach (var kvp in results)
                {
                    successes[kvp.Key] = kvp.Value;
                }
            }
        }

        public bool IsSuccess(int messageSeqNo)
        {
            var isSucc = false;
            successes.TryGetValue(messageSeqNo, out isSucc);
            return isSucc;
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            codec.WriteInt(totalSize);
            codec.WriteIntBooleanMap(successes);
        }

        protected override void Parse0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            totalSize = codec.ReadInt();
            successes = codec.ReadIntBooleanMap();
        }
    }
}
