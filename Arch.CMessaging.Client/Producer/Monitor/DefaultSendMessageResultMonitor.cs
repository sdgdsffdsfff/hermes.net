using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Arch.CMessaging.Client.Core.Ioc;
using Arch.CMessaging.Client.Core.Service;
using Arch.CMessaging.Client.Transport.Command;
using Freeway.Logging;
using Arch.CMessaging.Client.Producer.Config;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Message;
using Arch.CMessaging.Client.Core.Future;
using Arch.CMessaging.Client.Core.Result;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Producer.Sender;
using Com.Dianping.Cat;
using Com.Dianping.Cat.Message;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Com.Dianping.Cat.Message.Internals;

namespace Arch.CMessaging.Client.Producer.Monitor
{
    [Named(ServiceType = typeof(ISendMessageResultMonitor))]
    public class DefaultSendMessageResultMonitor : ISendMessageResultMonitor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultSendMessageResultMonitor));
        private ConcurrentDictionary<long, Pair<SendMessageCommand, SettableFuture<object>>> commands = new ConcurrentDictionary<long, Pair<SendMessageCommand, SettableFuture<object>>>();
        private long ticksOfLocalMinusUtc;

        #region ISendMessageResultMonitor Members

        public IFuture<object> Monitor(SendMessageCommand command)
        {
            SettableFuture<object> future = SettableFuture<object>.Create();
            commands[command.Header.CorrelationId] = new Pair<SendMessageCommand, SettableFuture<object>>(command, future);
            return future;
        }

        public void ResultReceived(SendMessageResultCommand result)
        {
            if (result != null)
            {
                Pair<SendMessageCommand, SettableFuture<object>> pair = null;
                commands.TryRemove(result.Header.CorrelationId, out pair);
                if (pair != null)
                {

                    try
                    {
                        SendMessageCommand sendMessageCommand = pair.Key;
                        SettableFuture<object> future = pair.Value;
                        future.Set(null);
                        sendMessageCommand.OnResultReceived(result);
                        Tracking(sendMessageCommand, true);
                    }
                    catch (Exception ex)
                    {
                        log.Warn(ex);
                    }
                }
            }
        }

        public void Cancel(SendMessageCommand cmd)
        {
            Pair<SendMessageCommand, SettableFuture<object>> pair = null;
            commands.TryRemove(cmd.Header.CorrelationId, out pair);
        }

        #endregion

        private void Tracking(SendMessageCommand sendMessageCommand, bool success)
        {

            string status = success ? CatConstants.SUCCESS : "Timeout";

            foreach (List<ProducerMessage> msgs in sendMessageCommand.ProducerMessages)
            {
                foreach (ProducerMessage msg in msgs)
                {
                    ITransaction t = Cat.NewTransaction("Message.Produce.Acked", msg.Topic);
                    IMessageTree tree = Cat.GetThreadLocalMessageTree();

                    String msgId = msg.GetDurableSysProperty(CatConstants.SERVER_MESSAGE_ID);
                    String parentMsgId = msg.GetDurableSysProperty(CatConstants.CURRENT_MESSAGE_ID);
                    String rootMsgId = msg.GetDurableSysProperty(CatConstants.ROOT_MESSAGE_ID);

                    tree.MessageId = msgId;
                    tree.ParentMessageId = parentMsgId;
                    tree.RootMessageId = rootMsgId;

                    ITransaction elapseT = Cat.NewTransaction("Message.Produce.Elapse", msg.Topic);
                    if (elapseT is DefaultTransaction)
                    {
                        // cat needs local mill of ticks
                        ((DefaultTransaction)elapseT).Timestamp = (ticksOfLocalMinusUtc + TimeExtension.UnixTimestampToTicks(msg.BornTime)) / TimeSpan.TicksPerMillisecond;
                        elapseT.AddData("command.message.count", sendMessageCommand.MessageCount);
                    }
                    elapseT.Status = status;
                    elapseT.Complete();

                    t.Status = status;
                    t.Complete();
                }

            }
        }
    }
}
