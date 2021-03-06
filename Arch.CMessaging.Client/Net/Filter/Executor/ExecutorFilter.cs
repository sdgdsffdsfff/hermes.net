﻿using System;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Write;

namespace Arch.CMessaging.Client.Net.Filter.Executor
{
    public class ExecutorFilter : IoFilterAdapter
    {
        private const IoEventType DefaultEventSet = IoEventType.ExceptionCaught |
            IoEventType.MessageReceived | IoEventType.MessageSent | IoEventType.SessionClosed |
            IoEventType.SessionIdle | IoEventType.SessionOpened;

        private readonly IoEventType _eventTypes;
        private readonly IoEventExecutor _executor;

        public ExecutorFilter()
            : this(null, DefaultEventSet)
        { }

        public ExecutorFilter(IoEventType eventTypes)
            : this(null, eventTypes)
        { }

        public ExecutorFilter(IoEventExecutor executor)
            : this(executor, DefaultEventSet)
        { }

        public ExecutorFilter(IoEventExecutor executor, IoEventType eventTypes)
        {
            _eventTypes = eventTypes;
            if (executor == null)
                _executor = new OrderedThreadPoolExecutor();
            else
                _executor = executor;
        }

        public IoEventExecutor Executor
        {
            get { return _executor; }
        }

        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. Create another instance and add it.");
        }

        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.SessionOpened) == IoEventType.SessionOpened)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionOpened, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionOpened(nextFilter, session);
            }
        }

        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.SessionClosed) == IoEventType.SessionClosed)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionClosed, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionClosed(nextFilter, session);
            }
        }

        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            if ((_eventTypes & IoEventType.SessionIdle) == IoEventType.SessionIdle)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionIdle, session, status);
                FireEvent(ioe);
            }
            else
            {
                base.SessionIdle(nextFilter, session, status);
            }
        }

        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            if ((_eventTypes & IoEventType.ExceptionCaught) == IoEventType.ExceptionCaught)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause);
                FireEvent(ioe);
            }
            else
            {
                base.ExceptionCaught(nextFilter, session, cause);
            }
        }

        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            if ((_eventTypes & IoEventType.MessageReceived) == IoEventType.MessageReceived)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.MessageReceived, session, message);
                FireEvent(ioe);
            }
            else
            {
                base.MessageReceived(nextFilter, session, message);
            }
        }

        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.MessageSent) == IoEventType.MessageSent)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.MessageSent(nextFilter, session, writeRequest);
            }
        }

        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.Write) == IoEventType.Write)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.Write, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.FilterWrite(nextFilter, session, writeRequest);
            }
        }

        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.Close) == IoEventType.Close)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.Close, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.FilterClose(nextFilter, session);
            }
        }

        protected void FireEvent(IoFilterEvent ioe)
        {
            _executor.Execute(ioe);
        }
    }
}
