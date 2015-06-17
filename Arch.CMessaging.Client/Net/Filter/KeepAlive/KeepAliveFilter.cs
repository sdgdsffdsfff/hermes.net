﻿using System;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Write;

namespace Arch.CMessaging.Client.Net.Filter.KeepAlive
{
    public class KeepAliveFilter : IoFilterAdapter
    {
        private readonly AttributeKey WAITING_FOR_RESPONSE;
        private readonly AttributeKey IGNORE_READER_IDLE_ONCE;

        private readonly IKeepAliveMessageFactory _messageFactory;
        private readonly IdleStatus _interestedIdleStatus;
        private volatile IKeepAliveRequestTimeoutHandler _requestTimeoutHandler;
        private volatile Int32 _requestInterval;
        private volatile Int32 _requestTimeout;
        private volatile Boolean _forwardEvent;

        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory)
            : this(messageFactory, IdleStatus.ReaderIdle, KeepAliveRequestTimeoutHandler.Close)
        { }

        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus)
            : this(messageFactory, interestedIdleStatus, KeepAliveRequestTimeoutHandler.Close)
        { }

        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, IdleStatus.ReaderIdle, strategy)
        { }

        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, interestedIdleStatus, strategy, 60, 30)
        { }

        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy, Int32 keepAliveRequestInterval, Int32 keepAliveRequestTimeout)
        {
            if (messageFactory == null)
                throw new ArgumentNullException("messageFactory");
            if (strategy == null)
                throw new ArgumentNullException("strategy");

            WAITING_FOR_RESPONSE = new AttributeKey(GetType(), "waitingForResponse");
            IGNORE_READER_IDLE_ONCE = new AttributeKey(GetType(), "ignoreReaderIdleOnce");
            _messageFactory = messageFactory;
            _interestedIdleStatus = interestedIdleStatus;
            _requestTimeoutHandler = strategy;
            RequestInterval = keepAliveRequestInterval;
            RequestTimeout = keepAliveRequestTimeout;
        }

        public Int32 RequestInterval
        {
            get { return _requestInterval; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestInterval must be a positive integer: " + value);
                _requestInterval = value;
            }
        }

        public Int32 RequestTimeout
        {
            get { return _requestTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestTimeout must be a positive integer: " + value);
                _requestTimeout = value;
            }
        }

        public Boolean ForwardEvent
        {
            get { return _forwardEvent; }
            set { _forwardEvent = value; }
        }

        public IKeepAliveRequestTimeoutHandler RequestTimeoutHandler
        {
            get { return _requestTimeoutHandler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _requestTimeoutHandler = value;
            }
        }

        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. "
                    + "Create another instance and add it.");
        }

        public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            try
            {
                if (_messageFactory.IsRequest(session, message))
                {
                    Object pongMessage = _messageFactory.GetResponse(session, message);

                    if (pongMessage != null)
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pongMessage));
                }

                if (_messageFactory.IsResponse(session, message))
                    ResetStatus(session);
            }
            finally
            {
                if (!IsKeepAliveMessage(session, message))
                    nextFilter.MessageReceived(session, message);
            }
        }

        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Object message = writeRequest.Message;
            if (!IsKeepAliveMessage(session, message))
                nextFilter.MessageSent(session, writeRequest);
        }

        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            if (status == _interestedIdleStatus)
            {
                if (!session.ContainsAttribute(WAITING_FOR_RESPONSE))
                {
                    Object pingMessage = _messageFactory.GetRequest(session);
                    if (pingMessage != null)
                    {
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pingMessage));
                        //session.Write(pingMessage);

                        // If policy is OFF, there's no need to wait for
                        // the response.
                        if (_requestTimeoutHandler != KeepAliveRequestTimeoutHandler.DeafSpeaker)
                        {
                            MarkStatus(session);
                            if (_interestedIdleStatus == IdleStatus.BothIdle)
                            {
                                session.SetAttribute(IGNORE_READER_IDLE_ONCE);
                            }
                        }
                        else
                        {
                            ResetStatus(session);
                        }
                    }
                }
                else
                {
                    HandlePingTimeout(session);
                }
            }
            else if (status == IdleStatus.ReaderIdle)
            {
                if (session.RemoveAttribute(IGNORE_READER_IDLE_ONCE) == null)
                {
                    if (session.ContainsAttribute(WAITING_FOR_RESPONSE))
                    {
                        HandlePingTimeout(session);
                    }
                }
            }

            if (_forwardEvent)
                nextFilter.SessionIdle(session, status);
        }

        private void ResetStatus(IoSession session)
        {
            session.Config.ReaderIdleTime = 0;
            session.Config.WriterIdleTime = 0;
            session.Config.SetIdleTime(_interestedIdleStatus, RequestInterval);
            session.RemoveAttribute(WAITING_FOR_RESPONSE);
        }

        private Boolean IsKeepAliveMessage(IoSession session, Object message)
        {
            return _messageFactory.IsRequest(session, message) || _messageFactory.IsResponse(session, message);
        }

        private void HandlePingTimeout(IoSession session)
        {
            ResetStatus(session);
            IKeepAliveRequestTimeoutHandler handler = _requestTimeoutHandler;
            if (handler == KeepAliveRequestTimeoutHandler.DeafSpeaker)
                return;
            handler.KeepAliveRequestTimedOut(this, session);
        }

        private void MarkStatus(IoSession session)
        {
            session.Config.SetIdleTime(_interestedIdleStatus, 0);
            session.Config.ReaderIdleTime = RequestTimeout;
            session.SetAttribute(WAITING_FOR_RESPONSE);
        }
    }
}
