using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.MetaEntity.Entity;
using Arch.CMessaging.Client.Newtonsoft.Json;
using Arch.CMessaging.Client.Core.Utils;
using Arch.CMessaging.Client.Core.Ioc;

namespace Arch.CMessaging.Client.Core.Message.Payload
{
    [Named(ServiceType = typeof(IPayloadCodec), ServiceName = Arch.CMessaging.Client.MetaEntity.Entity.Codec.JSON)]
    public class JsonPayloadCodec : AbstractPayloadCodec
    {
        #region IPayloadCodec Members

        public override string Type { get { return Arch.CMessaging.Client.MetaEntity.Entity.Codec.JSON; } }

        protected override byte[] DoEncode(string topic, object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        protected override object DoDecode(byte[] raw, Type type)
        {
            return JSON.DeserializeObject(raw, type);
        }

        #endregion
    }
}
