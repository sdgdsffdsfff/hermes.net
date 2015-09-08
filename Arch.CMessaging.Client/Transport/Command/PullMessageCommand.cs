using System;
using Arch.CMessaging.Client.Transport.Command;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Bo;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class PullMessageCommandV2 : AbstractCommand
    {

        public const int PULL_WITH_OFFSET = 1;
       
        public const int PULL_WITHOUT_OFFSET = 2;

        public int PullType { get; set; }

        private SettableFuture<PullMessageResultCommandV2> m_future;

        public PullMessageCommandV2()
            : this(PULL_WITHOUT_OFFSET, null, -1, null, null, 0, -1L)
        {
        }

        public PullMessageCommandV2(int type, String topic, int partition, String groupId, Offset offset, int size, long expireTime)
            : base(CommandType.MessagePull, 2)
        {
            PullType = type;
            Topic = topic;
            Partition = partition;
            GroupId = groupId;
            Offset = offset;
            Size = size;
            ExpireTime = expireTime;
        }

        public string GroupId { get; private set; }

        public string Topic { get; private set; }

        public int Partition { get; private set; }

        public Offset Offset { get; private set; }

        public int Size { get; private set; }

        public long ExpireTime { get; private set; }

        public SettableFuture<PullMessageResultCommandV2> getFuture()
        {
            return m_future;
        }

        public void setFuture(SettableFuture<PullMessageResultCommandV2> future)
        {
            m_future = future;
        }


        public void OnResultReceived(PullMessageResultCommandV2 ack)
        {
            m_future.Set(ack);
        }

        protected override void Parse0(IoBuffer buf)
        {
            throw new NotImplementedException();
        }

        protected override void  ToBytes0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);

            codec.WriteInt(PullType);
            codec.WriteString(Topic);
            codec.WriteInt(Partition);
            codec.WriteString(GroupId);
            codec.WriteOffset(Offset);
            codec.WriteInt(Size);
            codec.WriteLong(ExpireTime);
        }


    }
}

