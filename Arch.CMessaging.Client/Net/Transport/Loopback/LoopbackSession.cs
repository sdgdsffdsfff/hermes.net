using System;
using System.Collections.Concurrent;
using System.Net;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Net.Core.Filterchain;
using Arch.CMessaging.Client.Net.Core.Service;
using Arch.CMessaging.Client.Net.Core.Session;

namespace Arch.CMessaging.Client.Net.Transport.Loopback
{
    class LoopbackSession : AbstractIoSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("hermes-transport", "loopback", false, false, typeof(LoopbackEndPoint));

        private readonly LoopbackEndPoint _localEP;
        private readonly LoopbackEndPoint _remoteEP;
        private readonly LoopbackFilterChain _filterChain;
        private readonly ThreadSafeQueue<Object> _receivedMessageQueue;
        private readonly LoopbackSession _remoteSession;
        private readonly Object _lock;

        
        /// Constructor for client-side session.
        
        public LoopbackSession(IoService service, LoopbackEndPoint localEP,
            IoHandler handler, LoopbackPipe remoteEntry)
            : base(service)
        {
            Config = new DefaultLoopbackSessionConfig();
            _lock = new Byte[0];
            _localEP = localEP;
            _remoteEP = remoteEntry.Endpoint;
            _filterChain = new LoopbackFilterChain(this);
            _receivedMessageQueue = new ThreadSafeQueue<Object>();
            _remoteSession = new LoopbackSession(this, remoteEntry);
        }

        
        /// Constructor for server-side session.
        
        public LoopbackSession(LoopbackSession remoteSession, LoopbackPipe entry)
            : base(entry.Acceptor)
        {
            Config = new DefaultLoopbackSessionConfig();
            _lock = remoteSession._lock;
            _localEP = remoteSession._remoteEP;
            _remoteEP = remoteSession._localEP;
            _filterChain = new LoopbackFilterChain(this);
            _remoteSession = remoteSession;
            _receivedMessageQueue = new ThreadSafeQueue<Object>();
        }

        public override IoProcessor Processor
        {
            get { return _filterChain.Processor; }
        }

        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        public override EndPoint LocalEndPoint
        {
            get { return _localEP; }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return _remoteEP; }
        }

        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        public LoopbackSession RemoteSession
        {
            get { return _remoteSession; }
        }

        internal ThreadSafeQueue<Object> ReceivedMessageQueue
        {
            get { return _receivedMessageQueue; }
        }

        internal Object Lock
        {
            get { return _lock; }
        }
    }
}
