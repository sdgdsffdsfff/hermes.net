using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Net.Core.Future;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Transport.Command;
using Com.Dianping.Cat;
using System.Diagnostics;

namespace Arch.CMessaging.Client.Transport.EndPoint
{
    public class EndpointSession
    {
        private DefaultEndpointClient client;
        private BlockingQueue<WriteOp> opQueue;
        private ThreadSafe.Boolean flushing;
        private ThreadSafe.Boolean closed;
        private ThreadSafe.AtomicReference<WriteOp> flushingOp;
        private ThreadSafe.AtomicReference<IConnectFuture> sessionFuture;
        private ThreadSafe.Long flushCounter = new ThreadSafe.Long(0);
        private ThreadSafe.Long lastLogTime = new ThreadSafe.Long(0);

        public EndpointSession(DefaultEndpointClient client)
        {
            this.client = client;
            this.flushing = new ThreadSafe.Boolean(false);
            this.closed = new ThreadSafe.Boolean(false);
            this.flushingOp = new ThreadSafe.AtomicReference<WriteOp>(null);
            this.sessionFuture = new ThreadSafe.AtomicReference<IConnectFuture>(null);
            this.opQueue = new BlockingQueue<WriteOp>(client.Config.EndpointSessionSendBufferSize);
            
        }

        public void SetSessionFuture(IConnectFuture future)
        {
            if (!IsClosed)
                sessionFuture.WriteFullFence(future);
        }

        public void Write(ICommand command, int timeoutInMills)
        {
            if (!IsClosed)
            {
                if (!opQueue.Offer(new WriteOp(command, timeoutInMills, client)))
                {
                    var future = sessionFuture.ReadFullFence();
                    IoSession session = null;
                    if (future != null)
                        session = future.Session;
                    client.Log.Warn(string.Format("Send buffer of endpoint channel {0} is full", session == null ? "null" : session.RemoteEndPoint.ToString()));
                }
            }
        }

        public bool Flush()
        {
            if (!IsClosed)
            {
                PopExpiredOps();
                var future = sessionFuture.ReadFullFence();
                if (future != null)
                {
                    var session = future.Session;
                    if (session != null
                        && session.Connected
                        && !session.WriteSuspended && opQueue.Count != 0)
                    {
                        if (flushing.AtomicCompareExchange(true, false))
                        {
                            if (flushingOp.AtomicCompareExchange(opQueue.Peek(), null))
                            {
                                opQueue.Take();
                                DoFlush(session, flushingOp.ReadFullFence());
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void PopExpiredOps()
        {
            while (opQueue.Count > 0)
            {
                if (opQueue.Peek().IsExpired)
                {
                    opQueue.Take();
                }
                else
                {
                    break;
                }
            }
        }

        public bool HasUnflushOps { get { return opQueue.Count != 0 || flushingOp.ReadFullFence() != null; } }

        public bool IsFlushing { get { return flushing.ReadFullFence(); } }

        public bool IsClosed { get { return closed.ReadFullFence(); } }

        public void Close()
        {
            if (closed.AtomicCompareExchange(true, false))
            {
                var future = sessionFuture.ReadFullFence();
                if (future != null)
                    future.Session.Close(true);
            }
        }

        private void DoFlush(IoSession session, WriteOp op)
        {
            if (op != null && !op.IsExpired)
            {
                try
                {
                    long cnt = flushCounter.AtomicIncrementAndGet();
                    long now = new DateTime().CurrentTimeMillis();
                    if (now - lastLogTime.ReadFullFence() > 60000)
                    {
                        lastLogTime.WriteFullFence(now);
                        string log = String.Format("opQueueSize: {0}, session: {1}, couner: {2}", opQueue.Count, session.RemoteEndPoint.ToString(), cnt);
                        client.Log.Info(log);
                    }

                    var writeFuture = session.Write(op.Command);
                    writeFuture.Complete += (s, e) =>
                    {
                        if (e.Future.Done)
                        {
                            flushing.AtomicExchange(false);
                            flushingOp.AtomicExchange(null);
                        }
                        else
                        {
                            if (!IsClosed)
                            {
                                Thread.Sleep(client.Config.EndpointSessionWriteRetryDealyInMills);
                                DoFlush(e.Future.Session, op);
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    client.Log.Error(ex);
                    flushing.AtomicExchange(false);
                    flushingOp.AtomicExchange(null);
                }
            }
            else
            {
                flushing.AtomicExchange(false);
                flushingOp.AtomicExchange(null);
            }
        }

        private class WriteOp
        {
            private ICommand command;
            private long expireTime;
            private DefaultEndpointClient client;

            public WriteOp(ICommand command, int timeoutInMills, DefaultEndpointClient client)
            {
                this.client = client;
                this.command = command;
                this.expireTime = client.ClockService.Now() + timeoutInMills;
            }

            public ICommand Command { get { return command; } }

            public bool IsExpired { get { return expireTime < client.ClockService.Now(); } }
        }
    }
}
