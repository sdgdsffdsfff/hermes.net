﻿using System;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Write;

namespace Arch.CMessaging.Client.Net.Filter.Util
{
    public abstract class CommonEventFilter : IoFilterAdapter
    {
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionCreated, session, null));
        }
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionOpened, session, null));
        }
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionIdle, session, status));
        }

        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionClosed, session, null));
        }

        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause));
        }

        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageReceived, session, message));
        }

        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest));
        }
     
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Write, session, writeRequest));
        }

        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Close, session, null));
        }

        protected abstract void Filter(IoFilterEvent ioe);
    }
}
