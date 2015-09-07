using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Core.Utils;
using System.Dynamic;

namespace Arch.CMessaging.Client.Core.Message
{
    public class BaseConsumerMessage
    {
        public long BornTime { get; set; }

        public String RefKey { get; set; }

        public String Topic { get; set; }

        public object Body { get; set; }

        public PropertiesHolder PropertiesHolder{ get; set; }

        private ThreadSafe.AtomicReference<string> status = new ThreadSafe.AtomicReference<string>(MessageStatus.NOT_SET);

        public string Status
        {
            get{ return status.ReadFullFence(); }
        }

        public int RemainingRetries  { get; set; }

        public long OnMessageStartTimeMills { get; set; }

        public long OnMessageEndTimeMills { get; set; }

        public BaseConsumerMessage()
        {
            PropertiesHolder = new PropertiesHolder();
        }

        public bool Ack()
        {
            bool setSuccess = status.CompareAndSet(MessageStatus.NOT_SET, MessageStatus.SUCCESS);
            if (setSuccess)
            {
                OnMessageEndTimeMills = new DateTime().CurrentTimeMillis();
            }

            return setSuccess;
        }

        public bool Nack()
        {
            bool setSuccess = status.CompareAndSet(MessageStatus.NOT_SET, MessageStatus.FAIL);
            if (setSuccess)
            {
                OnMessageEndTimeMills = new DateTime().CurrentTimeMillis();
            }

            return setSuccess;
        }

        public void ResetStatus()
        {
            status.WriteFullFence(MessageStatus.NOT_SET);
        }

        public void AddDurableAppProperty(String name, String value)
        {
            PropertiesHolder.AddDurableAppProperty(name, value);
        }

        public void AddDurableSysProperty(String name, String value)
        {
            PropertiesHolder.AddDurableSysProperty(name, value);
        }

        public String GetDurableAppProperty(String name)
        {
            return PropertiesHolder.GetDurableAppProperty(name);
        }

        public String GetDurableSysProperty(String name)
        {
            return PropertiesHolder.GetDurableSysProperty(name);
        }

        public void AddVolatileProperty(String name, String value)
        {
            PropertiesHolder.AddVolatileProperty(name, value);
        }

        public String GetVolatileProperty(String name)
        {
            return PropertiesHolder.GetVolatileProperty(name);
        }

        public IEnumerator<string> RawDurableAppPropertyNames
        {
            get{ return PropertiesHolder.RawDurableAppPropertyNames.GetEnumerator(); }
        }

        public bool IsAck()
        {
            return status.ReadFullFence() != MessageStatus.FAIL;
        }

        public override string ToString()
        {
            return "BaseConsumerMessage{" + "m_bornTime=" + BornTime + ", m_refKey='" + RefKey + '\'' + ", m_topic='"
            + Topic + '\'' + ", m_body=" + Body + ", m_propertiesHolder=" + PropertiesHolder + ", m_status="
            + Status + ", m_remainingRetries=" + RemainingRetries + '}';
        }
    }
}
