using System;
using System.Collections.Concurrent;
using Arch.CMessaging.Client.Core.Collections;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Net.Core.Session;

namespace Arch.CMessaging.Client.Net.Filter.Codec.StateMachine
{
    public class DecodingStateProtocolDecoder : IProtocolDecoder
    {
        private readonly IDecodingState _state;
        private readonly ThreadSafeQueue<IoBuffer> _undecodedBuffers = new ThreadSafeQueue<IoBuffer>();
        private IoSession _session;

        public DecodingStateProtocolDecoder(IDecodingState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");
            _state = state;
        }

        public void Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            if (_session == null)
                _session = session;
            else if (_session != session)
                throw new InvalidOperationException(GetType().Name + " is a stateful decoder.  "
                        + "You have to create one per session.");

            _undecodedBuffers.Enqueue(input);
            while (true)
            {
                IoBuffer b;
                if (!_undecodedBuffers.TryPeek(out b))
                    break;

                Int32 oldRemaining = b.Remaining;
                _state.Decode(b, output);
                Int32 newRemaining = b.Remaining;
                if (newRemaining != 0)
                {
                    if (oldRemaining == newRemaining)
                        throw new InvalidOperationException(_state.GetType().Name
                            + " must consume at least one byte per decode().");
                }
                else
                {
                    _undecodedBuffers.TryDequeue(out b);
                }
            }
        }

        public void FinishDecode(IoSession session, IProtocolDecoderOutput output)
        {
            _state.FinishDecode(output);
        }

        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
