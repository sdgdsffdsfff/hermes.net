﻿using System;
using Arch.CMessaging.Client.Net.Core.Write;

namespace Arch.CMessaging.Client.Net.Core.Session
{

    public class IoEvent
    {
        private readonly IoEventType _eventType;
        private readonly IoSession _session;
        private readonly Object _parameter;

        
        
        public IoEvent(IoEventType eventType, IoSession session, Object parameter)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            _eventType = eventType;
            _session = session;
            _parameter = parameter;
        }

        public IoEventType EventType
        {
            get { return _eventType; }
        }

        public IoSession Session
        {
            get { return _session; }
        }

        public Object Parameter
        {
            get { return _parameter; }
        }

        public virtual void Fire()
        {
            switch (_eventType)
            {
                case IoEventType.MessageReceived:
                    _session.FilterChain.FireMessageReceived(_parameter);
                    break;
                case IoEventType.MessageSent:
                    _session.FilterChain.FireMessageSent((IWriteRequest)_parameter);
                    break;
                case IoEventType.Write:
                    _session.FilterChain.FireFilterWrite((IWriteRequest)_parameter);
                    break;
                case IoEventType.Close:
                    _session.FilterChain.FireFilterClose();
                    break;
                case IoEventType.ExceptionCaught:
                    _session.FilterChain.FireExceptionCaught((Exception)_parameter);
                    break;
                case IoEventType.SessionIdle:
                    _session.FilterChain.FireSessionIdle((IdleStatus)_parameter);
                    break;
                case IoEventType.SessionCreated:
                    _session.FilterChain.FireSessionCreated();
                    break;
                case IoEventType.SessionOpened:
                    _session.FilterChain.FireSessionOpened();
                    break;
                case IoEventType.SessionClosed:
                    _session.FilterChain.FireSessionClosed();
                    break;
                default:
                    throw new InvalidOperationException("Unknown event type: " + _eventType);
            }
        }

        
        public override String ToString()
        {
            if (_parameter == null)
                return "[" + _session + "] " + _eventType;
            else
                return "[" + _session + "] " + _eventType + ": " + _parameter;
        }
    }
}
