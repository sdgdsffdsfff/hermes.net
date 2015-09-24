using System;
using Avro.Specific;
using Arch.CMessaging.Client.Core.Ioc;
using Avro;
using Avro.Generic;
using System.IO;
using Avro.IO;
using Arch.CMessaging.Client.Core.MetaService.Internal;
using System.Collections.Generic;

namespace Arch.CMessaging.Client.Core.Message.Payload
{
    [Named(ServiceType = typeof(IPayloadCodec), ServiceName = Arch.CMessaging.Client.MetaEntity.Entity.Codec.AVRO)]
    public class AvroPayloadCodec : AbstractPayloadCodec, IInitializable
    {
        private const byte MAGIC_BYTE = 0x0;

        public override string Type { get { return Arch.CMessaging.Client.MetaEntity.Entity.Codec.AVRO; } }

        [Inject]
        private IMetaManager metaManager;

        SchemaCache schemaCache;

        public void Initialize()
        {
            schemaCache = new SchemaCache(metaManager);           
        }

        protected override byte[] DoEncode(string topic, object obj)
        {
            if (!(obj is ISpecificRecord))
            {
                throw new InvalidDataException(string.Format("{0} should be instance of ISpecificRecord to user Avro", typeof(object)));                
            }

            Schema schema = ((ISpecificRecord)obj).Schema;

            int schemaId = schemaCache.RegisterSchema(schema.ToString(), topic + "-value");
            if (!IsValidSchemaId(schemaId))
            {
                throw new InvalidDataException(string.Format("Avro schema of {0} is incompaible with latest schema", typeof(object)));                
            }


            SpecificDatumWriter<object> w = new SpecificDatumWriter<object>(schema);

            using (MemoryStream stream = new MemoryStream())
            {
                // same with io.confluent.kafka used by Java
                // prepend body with magic byte and schemaId
                stream.WriteByte(MAGIC_BYTE);
                byte[] schemaIdBytes = BitConverter.GetBytes(schemaId);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(schemaIdBytes);
                }
                foreach (byte b in schemaIdBytes)
                {
                    stream.WriteByte(b);
                }

                w.Write(obj, new BinaryEncoder(stream));

                stream.Seek(0, SeekOrigin.Begin);
                byte[] result = new byte[stream.Length];
                stream.Read(result, 0, result.Length);
                return result;
            }
        }

        protected override object DoDecode(byte[] raw, Type type)
        {
            using (MemoryStream stream = new MemoryStream(raw))
            {
                byte magicByte = (byte)stream.ReadByte();
                if (magicByte != MAGIC_BYTE)
                {
                    throw new InvalidDataException("Unknown magic byte!");                
                }

                byte[] schemaIdBytes = new byte[4];
                stream.Read(schemaIdBytes, 0, 4);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(schemaIdBytes);
                }

                int schemaId = BitConverter.ToInt32(schemaIdBytes, 0);
                Schema schema = Schema.Parse(schemaCache.GetSchemaString(schemaId));

                SpecificDatumReader<object> r = new SpecificDatumReader<object>(schema, schema);

                return r.Read(null, new BinaryDecoder(stream));
            }
        }

        private bool IsValidSchemaId(int id)
        {
            return id >= 0;
        }
    }

    class SchemaCache
    {
        private IMetaManager metaManager;

        private object idCacheLock = new object();

        private object schemaCacheLock = new object();

        private Dictionary<string, int> schemaIdCache = new Dictionary<string, int>();

        private Dictionary<int, string> schemaCache = new Dictionary<int, string>();

        public SchemaCache(IMetaManager metaManager)
        {
            this.metaManager = metaManager;
        }

        public int RegisterSchema(String schema, String subject)
        {
            lock (idCacheLock)
            {
                if (schemaIdCache.ContainsKey(subject))
                {
                    return schemaIdCache[subject];
                }
                else
                {
                    int schemaId = metaManager.GetMetaProxy().RegisterSchema(schema, subject);
                    schemaIdCache[subject] = schemaId;
                    return schemaId;
                }
            }
        }

        public string GetSchemaString(int schemaId)
        {
            lock (schemaCacheLock)
            {
                if (schemaCache.ContainsKey(schemaId))
                {
                    return schemaCache[schemaId];
                }
                else
                {
                    string schema = metaManager.GetMetaProxy().GetSchemaString(schemaId);
                    schemaCache[schemaId] = schema;
                    return schema;
                }
            }
        }

    }
}

