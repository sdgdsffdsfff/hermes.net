using System;
using Arch.CMessaging.Client.Transport.Command;
using System.Collections.Generic;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Core.Bo;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class PullMessageResultCommandV2 : AbstractCommand
    {

        public List<TppConsumerMessageBatch> Batches { get; set; }

        public Offset Offset { get; set; }

        public  IoSession Channel { get; set; }

        public PullMessageResultCommandV2()
            : base(CommandType.ResultMessagePull, 2)
        {
            Batches = new List<TppConsumerMessageBatch>();
        }

        public void addBatches(List<TppConsumerMessageBatch> newBatches)
        {
            if (newBatches != null)
            {
                Batches.AddRange(newBatches);
            }
        }

        protected override void Parse0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            List<TppConsumerMessageBatch> batches = new List<TppConsumerMessageBatch>();

            readBatchMetas(codec, batches);

            readBatchDatas(buf, codec, batches);

            Batches = batches;

            Offset = codec.ReadOffset();
        }

        private void readBatchDatas(IoBuffer buf, HermesPrimitiveCodec codec, List<TppConsumerMessageBatch> batches)
        {
            foreach (TppConsumerMessageBatch batch in batches)
            {
                int len = codec.ReadInt();
                batch.Data = buf.GetSlice(len);
            }

        }

        private void readBatchMetas(HermesPrimitiveCodec codec, List<TppConsumerMessageBatch> batches)
        {
            int batchSize = codec.ReadInt();
            for (int i = 0; i < batchSize; i++)
            {
                TppConsumerMessageBatch batch = new TppConsumerMessageBatch();
                int msgSize = codec.ReadInt();
                batch.Topic = codec.ReadString();
                batch.Partition = codec.ReadInt();
                batch.Priority = codec.ReadInt();
                batch.Resend = codec.ReadBoolean();

                for (int j = 0; j < msgSize; j++)
                {
                    batch.AddMessageMeta(new MessageMeta(codec.ReadLong(), codec.ReadInt(), codec.ReadLong(), codec.ReadInt(),
                            codec.ReadBoolean()));
                }
                batches.Add(batch);
            }
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            throw new NotImplementedException();
        }

    }
}

