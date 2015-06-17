﻿using System;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Core.Write;

namespace Arch.CMessaging.Client.Net.Filter.Util
{
    public class ReferenceCountingFilter : IoFilterAdapter
    {
        private readonly IoFilter _filter;
        private Int32 _count = 0;

        public ReferenceCountingFilter(IoFilter filter)
        {
            _filter = filter;
        }

        public override void Init()
        {
            // no-op, will init on-demand in pre-add if count == 0
        }
  
        public override void Destroy()
        {
            // no-op, will destroy on-demand in post-remove if count == 0
        }
      
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (_count == 0)
                _filter.Init();
            _count++;
            _filter.OnPreAdd(parent, name, nextFilter);
        }
       
        public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPostRemove(parent, name, nextFilter);
            _count--;
            if (_count == 0)
                _filter.Destroy();
        }
        
        public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPostAdd(parent, name, nextFilter);
        }
       
        public override void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPreRemove(parent, name, nextFilter);
        }
       
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionCreated(nextFilter, session);
        }
       
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionOpened(nextFilter, session);
        }
       
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionClosed(nextFilter, session);
        }
       
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            _filter.SessionIdle(nextFilter, session, status);
        }
       
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            _filter.ExceptionCaught(nextFilter, session, cause);
        }
       
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            _filter.MessageReceived(nextFilter, session, message);
        }
       
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            _filter.MessageSent(nextFilter, session, writeRequest);
        }
       
        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            _filter.FilterClose(nextFilter, session);
        }
        
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            _filter.FilterWrite(nextFilter, session, writeRequest);
        }
    }
}
