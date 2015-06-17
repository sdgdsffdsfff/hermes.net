﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Arch.CMessaging.Client.Net.Core.Service;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Util;

namespace Arch.CMessaging.Client.Net.Transport.Socket
{
    public class AsyncSocketAcceptor : AbstractSocketAcceptor
    {
        const Int32 opsToPreAlloc = 2;
        private BufferManager _bufferManager;
        private Pool<SocketAsyncEventArgsBuffer> _readWritePool;

        public AsyncSocketAcceptor()
            : this(1024)
        { }

        public AsyncSocketAcceptor(Int32 maxConnections)
            : base(maxConnections)
        {
            this.SessionDestroyed += OnSessionDestroyed;
        }

        
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            InitBuffer();
            return base.BindInternal(localEndPoints);
        }

        private void InitBuffer()
        {
            Int32 bufferSize = SessionConfig.ReadBufferSize;
            if (_bufferManager == null || _bufferManager.BufferSize != bufferSize)
            {
                // TODO free previous pool

                _bufferManager = new BufferManager(bufferSize * MaxConnections * opsToPreAlloc, bufferSize);
                _bufferManager.InitBuffer();

                var list = new List<SocketAsyncEventArgsBuffer>(MaxConnections * opsToPreAlloc);
                for (Int32 i = 0; i < MaxConnections * opsToPreAlloc; i++)
                {
                    SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                    _bufferManager.SetBuffer(readWriteEventArg);
                    SocketAsyncEventArgsBuffer buf = new SocketAsyncEventArgsBuffer(readWriteEventArg);
                    list.Add(buf);

                    readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(readWriteEventArg_Completed);
                }
                _readWritePool = new Pool<SocketAsyncEventArgsBuffer>(list);
            }
        }

        void readWriteEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            AsyncSocketSession session = e.UserToken as AsyncSocketSession;

            if (session == null)
                return;

            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                session.ProcessReceive(e);
            }
            else if (e.LastOperation == SocketAsyncOperation.Send
                || e.LastOperation == SocketAsyncOperation.SendPackets)
            {
                session.ProcessSend(e);
            }
            else
            {
                // TODO handle other Socket operations
            }
        }

        private void OnSessionDestroyed(Object sender, IoSessionEventArgs e)
        {
            AsyncSocketSession s = e.Session as AsyncSocketSession;
            if (s != null && _readWritePool != null)
            {
                _readWritePool.Push(s.ReadBuffer);
                _readWritePool.Push(s.WriteBuffer);
            }
        }

        
        protected override void BeginAccept(ListenerContext listener)
        {
            SocketAsyncEventArgs acceptEventArg = (SocketAsyncEventArgs)listener.Tag;
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.UserToken = listener;
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = listener.Socket.AcceptAsync(acceptEventArg);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(Object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndAccept(e.AcceptSocket, (ListenerContext)e.UserToken);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }
        }

        
        protected override IoSession NewSession(IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
        {
            SocketAsyncEventArgsBuffer readBuffer = _readWritePool.Pop();
            SocketAsyncEventArgsBuffer writeBuffer = _readWritePool.Pop();

            if (readBuffer == null)
            {
                readBuffer = (SocketAsyncEventArgsBuffer)
                    SocketAsyncEventArgsBufferAllocator.Instance.Allocate(SessionConfig.ReadBufferSize);
                readBuffer.SocketAsyncEventArgs.Completed += readWriteEventArg_Completed;
            }

            if (writeBuffer == null)
            {
                writeBuffer = (SocketAsyncEventArgsBuffer)
                    SocketAsyncEventArgsBufferAllocator.Instance.Allocate(SessionConfig.ReadBufferSize);
                writeBuffer.SocketAsyncEventArgs.Completed += readWriteEventArg_Completed;
            }

            return new AsyncSocketSession(this, processor, socket, readBuffer, writeBuffer, ReuseBuffer);
        }
    }
}
