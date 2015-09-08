using System;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Bo;
using Arch.CMessaging.Client.Core.Utils;
using System.Collections.Generic;
using Arch.CMessaging.Client.Net.Core.Buffer;

namespace Arch.CMessaging.Client.Transport.Command
{
    public class AckMessageCommandV2 : AbstractCommand
    {

        public const int NORMAL = 1;

        public const int FORWARD_ONLY = 2;

        // key: tpp, groupId, isResend
        public ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>> m_ackMsgSeqs{ get; set; }

        // key: tpp, groupId, isResend
        public ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>> m_nackMsgSeqs{ get; set; }

        public int Type { get; set; }

        public AckMessageCommandV2()
            : this(NORMAL)
        {
        }

        public AckMessageCommandV2(int type)
            : base(CommandType.MessageAck, 2)
        {
            m_ackMsgSeqs = new ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>>();
            m_nackMsgSeqs = new ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>>();
            Type = type;
        }

        protected override void ToBytes0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            writeMsgSeqMap(codec, m_ackMsgSeqs);
            writeMsgSeqMap(codec, m_nackMsgSeqs);
            codec.WriteInt(Type);
        }

        protected override void Parse0(IoBuffer buf)
        {
            HermesPrimitiveCodec codec = new HermesPrimitiveCodec(buf);
            m_ackMsgSeqs = readMsgSeqMap(codec);
            m_nackMsgSeqs = readMsgSeqMap(codec);
            Type = codec.ReadInt();
        }

        private void writeMsgSeqMap(HermesPrimitiveCodec codec,
                                    ConcurrentDictionary<Triple<Tpp, String, Boolean>, List<AckContext>> msgSeqMap)
        {
            if (msgSeqMap == null)
            {
                codec.WriteInt(0);
            }
            else
            {
                codec.WriteInt(msgSeqMap.Count);
                foreach (Triple<Tpp, String, Boolean> tppgr in msgSeqMap.Keys)
                {
                    Tpp tpp = tppgr.First;
                    codec.WriteString(tpp.Topic);
                    codec.WriteInt(tpp.Partition);
                    codec.WriteInt(tpp.Priority ? 0 : 1);
                    codec.WriteString(tppgr.Middle);
                    codec.WriteBoolean(tppgr.Last);
                }
                foreach (Triple<Tpp, String, Boolean> tppgr in msgSeqMap.Keys)
                {
                    List<AckContext> contexts = msgSeqMap[tppgr];

                    if (contexts == null || contexts.Count == 0)
                    {
                        codec.WriteInt(0);
                    }
                    else
                    {
                        codec.WriteInt(contexts.Count);
                        foreach (AckContext context in contexts)
                        {
                            codec.WriteLong(context.MsgSeq);
                            codec.WriteInt(context.RemainingRetries);
                            codec.WriteLong(context.OnMessageStartTimeMillis);
                            codec.WriteLong(context.OnMessageEndTimeMillis);
                        }
                    }
                }
            }

        }

        private ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>> readMsgSeqMap(HermesPrimitiveCodec codec)
        {
            ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>> msgSeqMap = new ConcurrentDictionary<Triple<Tpp, string, bool>, List<AckContext>>();

            int mapSize = codec.ReadInt();
            if (mapSize != 0)
            {
                List<Triple<Tpp, string, bool>> tppgrs = new List<Triple<Tpp, string, bool>>();
                for (int i = 0; i < mapSize; i++)
                {
                    Tpp tpp = new Tpp(codec.ReadString(), codec.ReadInt(), codec.ReadInt() == 0 ? true : false);
                    String groupId = codec.ReadString();
                    bool resend = codec.ReadBoolean();
                    tppgrs.Add(new Triple<Tpp, string, bool>(tpp, groupId, resend));
                }
                for (int i = 0; i < mapSize; i++)
                {
                    Triple<Tpp, string, bool> tppgr = tppgrs[i];

                    int len = codec.ReadInt();
                    if (len == 0)
                    {
                        msgSeqMap[tppgr] = new List<AckContext>();
                    }
                    else
                    {
                        msgSeqMap[tppgr] = new List<AckContext>(len);
                    }

                    for (int j = 0; j < len; j++)
                    {
                        msgSeqMap[tppgr].Add(new AckContext(codec.ReadLong(), codec.ReadInt(), codec.ReadLong(), codec.ReadLong()));
                    }
                }
            }

            return msgSeqMap;
        }

        public void addAckMsg(Tpp tpp, String groupId, bool resend, long msgSeq, int remainingRetries,
                              long onMessageStartTimeMillis, long onMessageEndTimeMillis)
        {
            Triple<Tpp, string, bool> key = new Triple<Tpp, string, bool>(tpp, groupId, resend);
            if (!m_ackMsgSeqs.ContainsKey(key))
            {
                m_ackMsgSeqs.TryAdd(key, new List<AckContext>());
            }
            m_ackMsgSeqs[key].Add(
                new AckContext(msgSeq, remainingRetries, onMessageStartTimeMillis, onMessageEndTimeMillis));
        }

        public void addNackMsg(Tpp tpp, String groupId, bool resend, long msgSeq, int remainingRetries,
                               long onMessageStartTimeMillis, long onMessageEndTimeMillis)
        {
            Triple<Tpp, string, bool> key = new Triple<Tpp, string, bool>(tpp, groupId, resend);
            if (!m_nackMsgSeqs.ContainsKey(key))
            {
                m_nackMsgSeqs.TryAdd(key, new List<AckContext>());
            }
            m_nackMsgSeqs[key].Add(
                new AckContext(msgSeq, remainingRetries, onMessageStartTimeMillis, onMessageEndTimeMillis));
        }

        public bool isEmpty()
        {
            return m_nackMsgSeqs.Count == 0 && m_ackMsgSeqs.Count == 0;
        }

        public class AckContext
        {
            public long MsgSeq { get; private set; }

            public int RemainingRetries { get; private set; }

            public long OnMessageStartTimeMillis { get; private set; }

            public long OnMessageEndTimeMillis { get; private set; }

            public AckContext(long msgSeq, int remainingRetries, long onMessageStartTimeMillis, long onMessageEndTimeMillis)
            {
                MsgSeq = msgSeq;
                RemainingRetries = remainingRetries;
                OnMessageStartTimeMillis = onMessageStartTimeMillis;
                OnMessageEndTimeMillis = onMessageEndTimeMillis;
            }

            public override string ToString()
            {
                return "AckContext [m_msgSeq=" + MsgSeq + ", m_remainingRetries=" + RemainingRetries
                + ", m_onMessageStartTimeMillis=" + OnMessageStartTimeMillis + ", m_onMessageEndTimeMillis="
                + OnMessageEndTimeMillis + "]";
            }

        }
    }
}

