using System;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Net.Core.Session;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Net.Filter.Codec
{
    public abstract class CumulativeProtocolDecoder : ProtocolDecoderAdapter
    {
        private readonly AttributeKey BUFFER = new AttributeKey(typeof(CumulativeProtocolDecoder), "buffer");

        
        
        protected CumulativeProtocolDecoder()
        { }

        
        public override void Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            if (!session.TransportMetadata.HasFragmentation)
            {
                while (input.HasRemaining)
                {
                    if (!DoDecode(session, input, output))
                        break;
                }

                return;
            }

            Boolean usingSessionBuffer = true;
            IoBuffer buf = session.GetAttribute<IoBuffer>(BUFFER);

            // If we have a session buffer, append data to that; otherwise
            // use the buffer read from the network directly.
            if (buf == null)
            {
                buf = input;
                usingSessionBuffer = false;
            }
            else
            {
                Boolean appended = false;
                // Make sure that the buffer is auto-expanded.
                if (buf.AutoExpand)
                {
                    try
                    {
                        buf.Put(input);
                        appended = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // A user called derivation method (e.g. slice()),
                        // which disables auto-expansion of the parent buffer.
                    }
                    catch (OverflowException)
                    {
                        // A user disabled auto-expansion.
                    }
                }

                if (appended)
                {
                    buf.Flip();
                }
                else
                {
                    // Reallocate the buffer if append operation failed due to
                    // derivation or disabled auto-expansion.
                    buf.Flip();

                    IoBuffer newBuf = IoBuffer.Allocate(buf.Remaining + input.Remaining);
                    newBuf.AutoExpand = true;
                    newBuf.Order = buf.Order;
                    newBuf.Put(buf);
                    newBuf.Put(input);
                    newBuf.Flip();
                    buf = newBuf;

                    // Update the session attribute.
                    session.SetAttribute(BUFFER, buf);
                }
            }

            while (true)
            {
                Int32 oldPos = buf.Position;
                Boolean decoded = DoDecode(session, buf, output);
                if (decoded)
                {
                    if (buf.Position == oldPos)
                        throw new InvalidOperationException("DoDecode() can't return true when buffer is not consumed.");
                    if (!buf.HasRemaining) break;
                    else
                    {
                        StoreRemainingInSession(buf, session);
                        return;
                    }
                }
                else
                {
                    break;
                }
            }

            // if there is any data left that cannot be decoded, we store
            // it in a buffer in the session and next time this decoder is
            // invoked the session buffer gets appended to
            if (buf.HasRemaining)
            {
                if (usingSessionBuffer && buf.AutoExpand)
                {
                    buf.Compact();
                }
                else
                {
                    StoreRemainingInSession(buf, session);
                }
            }
            else
            {
                if (usingSessionBuffer)
                {
                    RemoveSessionBuffer(session);
                }
            }
        }

        protected abstract Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output);

        protected void RemoveSessionBuffer(IoSession session)
        {
            session.RemoveAttribute(BUFFER);
        }

        private void StoreRemainingInSession(IoBuffer buf, IoSession session)
        {
            IoBuffer remainingBuf = IoBuffer.Allocate(buf.Capacity);
            remainingBuf.AutoExpand = true;

            remainingBuf.Order = buf.Order;
            remainingBuf.Put(buf);

            session.SetAttribute(BUFFER, remainingBuf);
        }
    }
}
