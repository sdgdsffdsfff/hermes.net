using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Future;
using Arch.CMessaging.Client.Net.Core.Service;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Client.Net.Transport.Socket;

namespace Arch.CMessaging.Client.Net
{
    public class Bootstrap
    {
        private AsyncSocketConnector connector;
        private HashSet<SessionOption> options;
        public Bootstrap()
        {
            this.options = new HashSet<SessionOption>();
            this.connector = new AsyncSocketConnector();
        }

        public Bootstrap Option(SessionOption option, object value)
        {
            if (option == null)
                throw new ArgumentNullException();
            option.Value = value;
            if (!options.Contains(option)) options.Add(option);
            return this;
        }

        public Bootstrap Handler(Action<RangeIoFilterChainBuilder> act)
        {
            act(new RangeIoFilterChainBuilder(connector.FilterChain));
            return this;
        }

        public Bootstrap Handler(IoHandlerAdapter handler)
        {
            if (handler != null)
                this.connector.Handler = handler;
            return this;
        }

        public Bootstrap OnSessionDestroyed(Action<IoSession> action)
        {   
            connector.SessionDestroyed += (o, e) => { if (action != null) action(e.Session); };
            return this;
        }

        public Bootstrap OnSessionClosed(Action<IoSession> action)
        {
            connector.SessionClosed += (o, e) => { if (action != null) action(e.Session); };
            return this;
        }

        public Bootstrap OnSessionCreated(Action<IoSession> action)
        {
            connector.SessionCreated += (o, e) => { if (action != null) action(e.Session); };
            return this;
        }

        public Bootstrap OnSessionOpened(Action<IoSession> action)
        {
            connector.SessionOpened += (o, e) => { if (action != null) action(e.Session); };
            return this;
        }

        public DefaultConnectFuture Connect(string ip, int port)
        {
            IPAddress addr = null;
            if (!IPAddress.TryParse(ip, out addr))
                throw new ArgumentException("invalid ip address => {0}", ip);
            return Connect(new IPEndPoint(addr, port));
        }

        public DefaultConnectFuture Connect(IPEndPoint endpoint)
        {
            foreach (var option in options)
            {
                switch (option.Name)
                {
                    case "TCP_NODELAY": connector.SessionConfig.NoDelay = Convert.ToBoolean(option.Value);
                        break;
                    case "SO_LINGER": connector.SessionConfig.SoLinger = Convert.ToInt32(option.Value);
                        break;
                    case "SO_SNDBUF": connector.SessionConfig.SendBufferSize = Convert.ToInt32(option.Value);
                        break;
                    case "SO_RCVBUF": connector.SessionConfig.ReceiveBufferSize = Convert.ToInt32(option.Value);
                        break;
                    case "SO_KEEPALIVE": connector.SessionConfig.KeepAlive = Convert.ToBoolean(option.Value);
                        break;
                    case "CONNECT_TIMEOUT_MILLIS": connector.ConnectTimeoutInMillis = Convert.ToInt64(option.Value);
                        break;
                    case "ANY_IDLE_TIME":
                        {
                            connector.SessionConfig.ReaderIdleTime = Convert.ToInt32(option.Value);
                            connector.SessionConfig.WriterIdleTime = Convert.ToInt32(option.Value);
                        }
                        break;
                    default: 
                        break;
                }
            }
            return connector.Connect(endpoint) as DefaultConnectFuture;
        }
    }
}
