using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Transport.Command;

namespace Arch.CMessaging.Client.Transport
{
    public class Header
    {   
        static long correlationId = -1;
        public Header()
        {
            this.CorrelationId = Interlocked.Increment(ref correlationId);
            this.Properties = new Dictionary<string, string>();
            this.Version = 1;
        }
        public long CorrelationId { get; set; }
        public int Version { get; set; }
        public CommandType? CommandType { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public void AddProperty(string key, string value) 
        {
            Properties[key] = value;
        }

        public void Parse(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            Magic.ReadAndCheckMagic(buf);
            Version = codec.ReadInt();
            CommandType = codec.ReadInt().ToCommandType(Version);
            CorrelationId = codec.ReadLong();
            Properties = codec.ReadStringStringMap();
        }

        public void ToBytes(IoBuffer buf)
        {
            var codec = new HermesPrimitiveCodec(buf);
            Magic.WriteMagic(buf);
            codec.WriteInt(Version);
            codec.WriteInt(CommandType.ToInt());
            codec.WriteLong(CorrelationId);
            codec.WriteStringStringMap(Properties);
        }

        public override string ToString()
        {
            return string.Format("Header [m_version={0}, m_type={1}, m_correlationId={2}, m_properties={3}",
                Version, CommandType, CorrelationId, Properties.AsString());
        }
    }
}
