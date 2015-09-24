using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arch.CMessaging.Client.Net.Core.Session
{
    public class SessionOption
    {
        public static SessionOption TCP_NODELAY = new SessionOption("TCP_NODELAY");
        public static SessionOption SO_LINGER = new SessionOption("SO_LINGER");
        public static SessionOption SO_SNDBUF = new SessionOption("SO_SNDBUF");
        public static SessionOption SO_RCVBUF = new SessionOption("SO_RCVBUF");
        public static SessionOption SO_KEEPALIVE = new SessionOption("SO_KEEPALIVE");
        public static SessionOption ANY_IDLE_TIME = new SessionOption("ANY_IDLE_TIME");
        public static SessionOption CONNECT_TIMEOUT_MILLIS = new SessionOption("CONNECT_TIMEOUT_MILLIS");

        public SessionOption(string name)
        {
            this.Name = name;
        }

        public string Name { get; private set; }
        public object Value { get; internal set; }
    }
}
