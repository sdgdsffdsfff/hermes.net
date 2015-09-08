using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Result;
using Arch.CMessaging.Client.Core.Exceptions;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class SendMessageCommand : AbstractCommand
    {
        private const long serialVersionUID = 8443575812437722822L;
        private ThreadSafe.Long expireTime;
        private ThreadSafe.Integer msgCounter;
        private ConcurrentDictionary<int, IList<ProducerMessage>> messages;
        private Dictionary<int, SettableFuture<SendResult>> futures;
        private bool m_accepted = false;
        private long m_acceptedTime = -1L;
        private readonly object synLock = new object();

        public SendMessageCommand()
            : this(null, -1)
        {
        }

        public SendMessageCommand(string topic, int partition)
            : base(CommandType.MessageSend, 1)
        {
            this.Topic = topic;
            this.Partition = partition;
            this.expireTime = new ThreadSafe.Long(0);
            this.msgCounter = new ThreadSafe.Integer(0);
            this.messages = new ConcurrentDictionary<int, IList<ProducerMessage>>();
            this.futures = new Dictionary<int, SettableFuture<SendResult>>();
        }

        public string Topic { get; private set; }

        public int Partition { get; private set; }

        public int MessageCount { get { return msgCounter.ReadFullFence(); } }

        public IEnumerable<IList<ProducerMessage>> ProducerMessages { get { return messages.Values; } }

        public void Accepted(long acceptedTime)
        {
            lock (synLock)
            {
                m_acceptedTime = acceptedTime;
                m_accepted = true;
            }
        }

        public bool isExpired(long now, long timeoutMillis)
        {
            lock (synLock)
            {
                return m_accepted && (now - m_acceptedTime > timeoutMillis);
            }
        }

        public void AddMessage(ProducerMessage message, SettableFuture<SendResult> future)
        {
            Validate(message);
            message.SequenceNo = msgCounter.AtomicIncrementAndGet();
            if (message.IsPriority)
            {
                messages.TryAdd(0, new List<ProducerMessage>());
                messages[0].Add(message);
            }
            else
            {
                messages.TryAdd(1, new List<ProducerMessage>());
                messages[1].Add(message);
            }

            futures[message.SequenceNo] = future;
        }

        public void OnResultReceived(SendMessageResultCommand result)
        {
            foreach (var entry in futures)
            {
                if (result.IsSuccess(entry.Key))
                    entry.Value.Set(new SendResult());
                else
                    entry.Value.SetException(new MessageSendException("Send failed"));
            }
        }

        protected override void Parse0(IoBuffer buf)
        {
            Buf = buf;
            var codec = new HermesPrimitiveCodec(buf);
            msgCounter.WriteFullFence(codec.ReadInt());
            Topic = codec.ReadString();
            Partition = codec.ReadInt();
            
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            codec.WriteInt(msgCounter.ReadFullFence());
            codec.WriteString(Topic);
            codec.WriteInt(Partition);
            WriteDatas(buf, codec, messages);
        }

        private void WriteDatas(IoBuffer buf, HermesPrimitiveCodec codec, IDictionary<int, IList<ProducerMessage>> messages)
        {
            codec.WriteInt(messages.Count);
            foreach (var kvp in messages)
            {
                codec.WriteInt(kvp.Key);
                WriteMessages(kvp.Value, codec, buf);
            }
        }

        private void WriteMessages(IList<ProducerMessage> list, HermesPrimitiveCodec codec, IoBuffer buf)
        {
            var msgCodec = ComponentLocator.Lookup<IMessageCodec>();
            codec.WriteInt(list.Count);

            //seqNos
            foreach (var message in list)
                codec.WriteInt(message.SequenceNo);

            // placeholder for payload len
            var indexBeforeLen = buf.Position;
            codec.WriteInt(-1);

            var indexBeforePayload = buf.Position;
            //payload
            foreach (var message in list)
                msgCodec.Encode(message, buf);
            var indexAfterPayload = buf.Position;
            var payloadLen = indexAfterPayload - indexBeforePayload;

            // refill payload len
            buf.Position = indexBeforeLen;
            codec.WriteInt(payloadLen);

            buf.Position = indexAfterPayload;
        }

        /// <summary>
        /// Read Datas
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="codec"></param>
        /// <param name="topic"></param>
        /// <remarks>
        /// implements only in broker
        /// </remarks>
        private void ReadDatas(ByteBuffer buf, HermesPrimitiveCodec codec, string topic)
        {
            var size = codec.ReadInt();
            for (int i = 0; i < size; i++)
            {
                var priority = codec.ReadInt();
                //todo
            }
        }

        private void Validate(ProducerMessage message)
        {
            if (string.IsNullOrEmpty(Topic))
                throw new ArgumentNullException("SendMessageCommand.Topic");

            if (string.IsNullOrEmpty(message.Topic))
                throw new ArgumentNullException("ProducerMessage.Topic");

            if (!Topic.Equals(message.Topic, StringComparison.OrdinalIgnoreCase)
                || Partition != message.Partition)
            {
                throw new ArgumentException(string.Format("Illegal message[topic={0}, partition={1}] try to add to SendMessageCommand[topic={0}, partition={1}]",
                        message.Topic, message.Partition, Topic, Partition));
            }
        }

        public ICollection<IList<ProducerMessage>> GetProducerMessages()
        {
            return messages.Values;
        }

        public List<Pair<ProducerMessage, SettableFuture<SendResult>>> GetProducerMessageFuturePairs()
        {
            List<Pair<ProducerMessage, SettableFuture<SendResult>>> pairs = new List<Pair<ProducerMessage, SettableFuture<SendResult>>>();
            ICollection<IList<ProducerMessage>> msgsList = GetProducerMessages();
            foreach (IList<ProducerMessage> msgs in msgsList)
            {
                foreach (ProducerMessage msg in msgs)
                {
                    SettableFuture<SendResult> future;
                    futures.TryGetValue(msg.SequenceNo, out future);
                    if (future != null)
                    {
                        pairs.Add(new Pair<ProducerMessage, SettableFuture<SendResult>>(msg, future));
                    }
                }
            }

            return pairs;
        }

    }
}
