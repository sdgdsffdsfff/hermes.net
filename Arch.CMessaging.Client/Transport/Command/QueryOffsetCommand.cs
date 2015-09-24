using System;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class QueryOffsetCommand : AbstractCommand
    {
        public string Topic { get; set; }

        public int Partition{ get; set; }

        public string GroupId{ get; set; }

        public SettableFuture<QueryOffsetResultCommand> Future { get; set; }

        public QueryOffsetCommand()
            : this(null, -1, null)
        {
        }

        public QueryOffsetCommand(String topic, int partition, String groupId)
            : base(CommandType.QueryOffset, 1)
        {
            Topic = topic;
            Partition = partition;
            GroupId = groupId;
        }

        public void OnResultReceived(QueryOffsetResultCommand result)
        {
            Future.Set(result);
        }

        protected override void Parse0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            Topic = codec.ReadString();
            Partition = codec.ReadInt();
            GroupId = codec.ReadString();
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            codec.WriteString(Topic);
            codec.WriteInt(Partition);
            codec.WriteString(GroupId);
        }

    }
}

