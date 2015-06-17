﻿using System;
using System.Threading;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Future;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Net.Util;

namespace Arch.CMessaging.Client.Net.Core.Service
{
    public abstract class AbstractIoService : IoService, IoServiceSupport, IDisposable
    {
        private Int32 _active = 0;
        private DateTime _activationTime;
        private IoHandler _handler;
        private Boolean _hasHandler;
        private readonly IoSessionConfig _sessionConfig;
        private readonly IoServiceStatistics _stats;
        private IoFilterChainBuilder _filterChainBuilder = new DefaultIoFilterChainBuilder();
        private IoSessionDataStructureFactory _sessionDataStructureFactory = new DefaultIoSessionDataStructureFactory();
        private Boolean _disposed;
        private ConcurrentDictionary<Int64, IoSession> _managedSessions = new ConcurrentDictionary<Int64, IoSession>();
        public event EventHandler Activated;
        public event EventHandler<IdleEventArgs> Idle;
        public event EventHandler Deactivated;
        public event EventHandler<IoSessionEventArgs> SessionCreated;
        public event EventHandler<IoSessionEventArgs> SessionOpened;
        public event EventHandler<IoSessionEventArgs> SessionClosed;
        public event EventHandler<IoSessionEventArgs> SessionDestroyed;
        public event EventHandler<IoSessionIdleEventArgs> SessionIdle;
        public event EventHandler<IoSessionExceptionEventArgs> ExceptionCaught;
        public event EventHandler<IoSessionEventArgs> InputClosed;
        public event EventHandler<IoSessionMessageEventArgs> MessageReceived;
        public event EventHandler<IoSessionMessageEventArgs> MessageSent;
        
        protected AbstractIoService(IoSessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;
            _handler = new InnerHandler(this);
            _stats = new IoServiceStatistics(this);
        }

        
        public abstract ITransportMetadata TransportMetadata { get; }
        public Boolean Disposed { get { return _disposed; } }
        public IoHandler Handler
        {
            get { return _handler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _handler = value;
                _hasHandler = true;
            }
        }
        public IDictionary<Int64, IoSession> ManagedSessions
        {
            get { return _managedSessions; }
        }

        public IoSessionConfig SessionConfig
        {
            get { return _sessionConfig; }
        }

        public IoFilterChainBuilder FilterChainBuilder
        {
            get { return _filterChainBuilder; }
            set { _filterChainBuilder = value; }
        }

        public DefaultIoFilterChainBuilder FilterChain
        {
            get { return _filterChainBuilder as DefaultIoFilterChainBuilder; }
        }

        public IoSessionDataStructureFactory SessionDataStructureFactory
        {
            get { return _sessionDataStructureFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else if (Active)
                    throw new InvalidOperationException();
                _sessionDataStructureFactory = value;
            }
        }
        public Boolean Active
        {
            get { return _active > 0; }
        }
        public DateTime ActivationTime
        {
            get { return _activationTime; }
        }
        public IoServiceStatistics Statistics
        {
            get { return _stats; }
        }
        public IEnumerable<IWriteFuture> Broadcast(Object message)
        {
            List<IWriteFuture> answer = new List<IWriteFuture>(_managedSessions.Count);
            IoBuffer buf = message as IoBuffer;
            if (buf == null)
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(message));
                }
            }
            else
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(buf.Duplicate()));
                }
            }
            return answer;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void InitSession<TFuture>(IoSession session, TFuture future, Action<IoSession, TFuture> initializeSession)
            where TFuture : IoFuture
        {
            AbstractIoSession s = session as AbstractIoSession;
            if (s != null)
            {
                s.AttributeMap = s.Service.SessionDataStructureFactory.GetAttributeMap(session);
                s.SetWriteRequestQueue(s.Service.SessionDataStructureFactory.GetWriteRequestQueue(session));
            }

            if (future != null && future is IConnectFuture)
                session.SetAttribute(DefaultIoFilterChain.SessionCreatedFuture, future);

            if (initializeSession != null)
                initializeSession(session, future);

            FinishSessionInitialization0(session, future);
        }

        protected virtual void FinishSessionInitialization0(IoSession session, IoFuture future)
        {
            // Do nothing. Extended class might add some specific code 
        }

        protected virtual void Dispose(Boolean disposing)
        {
            _disposed = true;
        }

        private void DisconnectSessions()
        {
            IoAcceptor acceptor = this as IoAcceptor;
            if (acceptor == null)
                // We don't disconnect sessions for anything but an IoAcceptor
                return;

            if (!acceptor.CloseOnDeactivation)
                return;

            List<ICloseFuture> closeFutures = new List<ICloseFuture>(_managedSessions.Count);
            foreach (IoSession s in _managedSessions.Values)
            {
                closeFutures.Add(s.Close(true));
            }

            new CompositeIoFuture<ICloseFuture>(closeFutures).Await();
        }

        #region IoServiceSupport
        
        void IoServiceSupport.FireServiceActivated()
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) > 0)
                // The instance is already active
                return;
            _activationTime = DateTime.Now;
            _stats.LastReadTime = _activationTime;
            _stats.LastWriteTime = _activationTime;
            _stats.LastThroughputCalculationTime = _activationTime;
            DelegateUtils.SaveInvoke(Activated, this);
        }

        void IoServiceSupport.FireServiceIdle(IdleStatus idleStatus)
        {
            DelegateUtils.SaveInvoke(Idle, this, new IdleEventArgs(idleStatus));
        }

        void IoServiceSupport.FireSessionCreated(IoSession session)
        {
            // If already registered, ignore.
            if (!_managedSessions.TryAdd(session.Id, session))
                return;

            // Fire session events.
            IoFilterChain filterChain = session.FilterChain;
            filterChain.FireSessionCreated();
            filterChain.FireSessionOpened();

            if (_hasHandler)
                DelegateUtils.SaveInvoke(SessionCreated, this, new IoSessionEventArgs(session));
        }

        void IoServiceSupport.FireSessionDestroyed(IoSession session)
        {
            IoSession s;
            if (!_managedSessions.TryRemove(session.Id, out s))
                return;

            // Fire session events.
            session.FilterChain.FireSessionClosed();

            DelegateUtils.SaveInvoke(SessionDestroyed, this, new IoSessionEventArgs(session));

            // Fire a virtual service deactivation event for the last session of the connector.
            if (session.Service is IoConnector)
            {
                Boolean lastSession = _managedSessions.IsEmpty;
                if (lastSession)
                    ((IoServiceSupport)this).FireServiceDeactivated();
            }
        }

        void IoServiceSupport.FireServiceDeactivated()
        {
            if (Interlocked.CompareExchange(ref _active, 0, 1) == 0)
                // The instance is already desactivated
                return;
            DelegateUtils.SaveInvoke(Deactivated, this);
            DisconnectSessions();
        }

        #endregion

        class InnerHandler : IoHandler
        {
            private readonly AbstractIoService _service;

            public InnerHandler(AbstractIoService service)
            {
                _service = service;
            }

            public void SessionCreated(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionCreated;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionOpened(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionOpened;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionClosed(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionClosed;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionIdle(IoSession session, IdleStatus status)
            {
                EventHandler<IoSessionIdleEventArgs> act = _service.SessionIdle;
                if (act != null)
                    act(_service, new IoSessionIdleEventArgs(session, status));
            }

            void IoHandler.ExceptionCaught(IoSession session, Exception cause)
            {
                EventHandler<IoSessionExceptionEventArgs> act = _service.ExceptionCaught;
                if (act != null)
                    act(_service, new IoSessionExceptionEventArgs(session, cause));
            }

            void IoHandler.MessageReceived(IoSession session, Object message)
            {
                EventHandler<IoSessionMessageEventArgs> act = _service.MessageReceived;
                if (act != null)
                    act(_service, new IoSessionMessageEventArgs(session, message));
            }

            void IoHandler.MessageSent(IoSession session, Object message)
            {
                EventHandler<IoSessionMessageEventArgs> act = _service.MessageSent;
                if (act != null)
                    act(_service, new IoSessionMessageEventArgs(session, message));
            }

            void IoHandler.InputClosed(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.InputClosed;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
                else
                    session.Close(true);
            }
        }
    }
}
