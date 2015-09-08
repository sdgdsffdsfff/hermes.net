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
    public class DefaultSendMessageResultMonitor : ISendMessageResultMonitor, IInitializable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultSendMessageResultMonitor));
        private ConcurrentDictionary<long, SendMessageCommand> commands = new ConcurrentDictionary<long, SendMessageCommand>();
        private object syncRoot = new object();
        private Timer timer;

        [Inject]
        private ISystemClockService systemClockService;

        [Inject]
        private ProducerConfig config;

        #region ISendMessageResultMonitor Members

        public void Monitor(SendMessageCommand command)
        {
            if (command != null)
            {
                lock (syncRoot)
                {
                    commands[command.Header.CorrelationId] = command;
                }
            }
        }

        public void ResultReceived(SendMessageResultCommand result)
        {
            if (result != null)
            {
                SendMessageCommand sendMessageCommand = null;
                lock (syncRoot)
                {
                    var correlationId = result.Header.CorrelationId;
                    if (commands.TryGetValue(correlationId, out sendMessageCommand))
                    {
                        SendMessageCommand oldCommand;
                        commands.TryRemove(correlationId, out oldCommand);
                    }
                }
                if (sendMessageCommand != null)
                {
                    try
                    {
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
                        ((DefaultTransaction)elapseT).Timestamp = msg.BornTime;
                        elapseT.AddData("command.message.count", sendMessageCommand.MessageCount);
                    }
                    elapseT.Status = status;
                    elapseT.Complete();

                    t.Status = status;
                    t.Complete();
                }
            }
        }

        #region IInitializable Members

        public void Initialize()
        {
            timer = new Timer(TimeoutCheck, null, 5, Timeout.Infinite);
        }

        #endregion

        private void TimeoutCheck(object state)
        {
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                ScanAndResendTimeoutCommands();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            finally
            {
                timer.Change(5, Timeout.Infinite);
            }
        }

        protected void ScanAndResendTimeoutCommands()
        {
            List<SendMessageCommand> timeoutCmds = ScanTimeoutCommands();

            if (timeoutCmds.Count != 0)
            {
                Resend(timeoutCmds);
            }
        }

        protected List<SendMessageCommand> ScanTimeoutCommands()
        {
            List<SendMessageCommand> timeoutCmds = new List<SendMessageCommand>();
            lock (syncRoot)
            {
                foreach (KeyValuePair<long, SendMessageCommand> entry in commands)
                {
                    SendMessageCommand cmd = entry.Value;
                    long correlationId = entry.Key;
                    if (cmd.isExpired(systemClockService.Now(), config.SendMessageReadResultTimeoutMillis))
                    {
                        SendMessageCommand oldCmd;
                        commands.TryRemove(correlationId, out oldCmd);
                        timeoutCmds.Add(cmd);
                    }
                }
            }
            return timeoutCmds;
        }

        protected void Resend(List<SendMessageCommand> timeoutCmds)
        {
            IMessageSender messageSender = ComponentLocator.Lookup<IMessageSender>(Endpoint.BROKER);
            if (messageSender != null)
            {
                messageSender.Resend(timeoutCmds);
            }
        }
    }
}
