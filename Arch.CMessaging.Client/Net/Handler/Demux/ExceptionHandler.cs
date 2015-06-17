﻿using System;
using Arch.CMessaging.Client.Net.Core.Session;

namespace Arch.CMessaging.Client.Net.Handler.Demux
{
    public class ExceptionHandler<E> : IExceptionHandler<E> where E : Exception
    {
        public static readonly IExceptionHandler<Exception> Noop = new NoopExceptionHandler();
        public static readonly IExceptionHandler<Exception> Close = new CloseExceptionHandler();
        private readonly Action<IoSession, E> _act;

        public ExceptionHandler()
        { }

        public ExceptionHandler(Action<IoSession, E> act)
        {
            if (act == null)
                throw new ArgumentNullException("act");
            _act = act;
        }

        public virtual void ExceptionCaught(IoSession session, E cause)
        {
            if (_act != null)
                _act(session, cause);
        }

        void IExceptionHandler.ExceptionCaught(IoSession session, Exception cause)
        {
            ExceptionCaught(session, (E)cause);
        }
    }

    class NoopExceptionHandler : IExceptionHandler<Exception>
    {
        internal NoopExceptionHandler() { }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            // Do nothing
        }
    }

    class CloseExceptionHandler : IExceptionHandler<Exception>
    {
        internal CloseExceptionHandler() { }

        public void ExceptionCaught(IoSession session, Exception cause)
        {
            session.Close(true);
        }
    }
}
