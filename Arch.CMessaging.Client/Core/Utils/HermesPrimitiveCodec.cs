using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Net.Core.Buffer;
using Arch.CMessaging.Client.Core.Bo;

namespace Arch.CMessaging.Client.Core.Utils
{
    public class HermesPrimitiveCodec
    {
        private IoBuffer buf;
        private const byte NULL_VALUE = 255;

        public HermesPrimitiveCodec(IoBuffer buf)
        {
            this.buf = buf;
        }

        public int ReadInt()
        {
            return buf.GetInt32();
        }

        public char ReadChar()
        {
            return buf.GetChar();
        }

        public long ReadLong()
        {
            return buf.GetInt64();
        }



        public bool ReadBoolean()
        {
            return buf.Get() != 0;
        }

        public byte[] ReadBytes()
        {
            var firstByte = buf.Get();
            if (firstByte == NULL_VALUE)
                return null;
            else
            {
                ReadIndexBack(buf, 1);
                var length = buf.GetInt32();
                var array = new byte[length];
                buf.Get(array, 0, array.Length);
                return array;
            }
        }

        public string ReadString()
        {
            string value = null;
            var strBytes = ReadBytes();
            if (strBytes != null)
                value = Encoding.UTF8.GetString(strBytes);
            return value;
        }

        public Dictionary<string, string> ReadStringStringMap()
        {
            var firstByte = buf.Get();
            if (firstByte == NULL_VALUE)
                return null;
            else
            {
                ReadIndexBack(buf, 1);
                var length = buf.GetInt32();
                Dictionary<string, string> result = new Dictionary<string,string>();
                if (length > 0)
                {
                    for (var i = 0; i < length; i++)
                        result[ReadString()] = ReadString();
                }
                return result;
            }
        }

        public Dictionary<long, int> ReadLongIntMap()
        {
            var firstByte = buf.Get();
            if (firstByte == NULL_VALUE)
                return null;
            else
            {
                ReadIndexBack(buf, 1);
                var length = buf.GetInt32();
                Dictionary<long, int> result = new Dictionary<long, int>();
                if (length > 0)
                {
                    for (int i = 0; i < length; i++)
                    {
                        result[ReadLong()] = ReadInt();
                    }
                }
                return result;
            }
        }

        public Dictionary<int, bool> ReadIntBooleanMap()
        {
            var firstByte = buf.Get();
            if (firstByte == NULL_VALUE)
                return null;
            else
            {
                ReadIndexBack(buf, 1);
                int length = buf.GetInt32();
                Dictionary<int, bool> result = new Dictionary<int, bool>();
                if (length > 0)
                {
                    for (int i = 0; i < length; i++)
                    {
                        result[ReadInt()] = ReadBoolean();
                    }
                }
                return result;
            }
        }

        public void WriteBoolean(bool b)
        {
            buf.Put(b ? (byte)1 : (byte)0);
        }

        public void WriteBytes(byte[] bytes)
        {
            if (null == bytes)
                WriteNull();
            else
            {
                var length = bytes.Length;
                buf.PutInt32(length);
                buf.Put(bytes);
            }
        }

        public void WriteBytes(IoBuffer buf)
        {
            if (null == buf)
                WriteNull();
            else
            {
                buf.Flip();
                var length = buf.Remaining;
                buf.PutInt32(length);
                buf.Put(buf);
            }
        }

        public void WriteChar(char c)
        {
            buf.PutChar(c);
        }

        public void WriteInt(int val)
        {
            buf.PutInt32(val);
        }

        public void WriteLong(long val)
        {
            buf.PutInt64(val);
        }

        public void WriteString(string val)
        {
            if (string.IsNullOrEmpty(val))
                WriteNull();
            else
            {
                var bytes = Encoding.UTF8.GetBytes(val);
                var length = bytes.Length;
                buf.PutInt32(length);
                buf.Put(bytes);
            }
        }

        public void WriteStringStringMap(Dictionary<string, string> map)
        {
            if (null == map)
                WriteNull();
            else
            {
                buf.PutInt32(map.Count);
                if (map.Count > 0)
                {
                    foreach (var kvp in map)
                    {
                        WriteString(kvp.Key);
                        WriteString(kvp.Value);
                    }
                }
            }
        }

        public void WriteLongIntMap(Dictionary<long, int> map)
        {
            if (null == map)
                WriteNull();
            else
            {
                buf.PutInt32(map.Count);
                if (map.Count > 0)
                {
                    foreach (var kvp in map)
                    {
                        WriteLong(kvp.Key);
                        WriteInt(kvp.Value);
                    }
                }
            }
        }

        public void WriteIntBooleanMap(Dictionary<int, bool> map)
        {
            if (null == map)
                WriteNull();
            else
            {
                buf.PutInt32(map.Count);
                if (map.Count > 0)
                {
                    foreach (var kvp in map)
                    {
                        WriteInt(kvp.Key);
                        WriteBoolean(kvp.Value);
                    }
                }
            }
        }

        public void WriteNull()
        {
            buf.Put(NULL_VALUE);
        }

        public IoBuffer GetBuffer()
        {
            return buf;
        }

        private void ReadIndexBack(IoBuffer buf, int i)
        {
            if (buf.Position - i >= 0)
                buf.Position = buf.Position - i;
        }

        public Offset ReadOffset()
        {
            byte firstByte = buf.Get();
            if (NULL_VALUE == firstByte)
            {
                return null;
            }
            else
            {
                ReadIndexBack(buf, 1);
                long pOff = ReadLong();
                long npOff = ReadLong();
                DateTime rDate = new DateTime(ReadLong());
                long rOff = ReadLong();
                return new Offset(pOff, npOff, new Pair<DateTime, long>(rDate, rOff));
            }
        }

        public void WriteOffset(Offset offset)
        {
            if (offset == null)
            {
                WriteNull();
            }
            else
            {
                WriteLong(offset.PriorityOffset);
                WriteLong(offset.NonPriorityOffset);
                WriteLong(offset.ResendOffset.Key.CurrentTimeMillis());
                WriteLong(offset.ResendOffset.Value);
            }
        }
    }
}
