using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Core.Result;

namespace Arch.CMessaging.Client.Core.Message
{
    public class ProducerMessage
    {
        private object body;

        public ProducerMessage(string topic, object body)
        {
            this.Topic = topic;
            this.body = body;
            WithHeader = true;
            this.PropertiesHolder = new PropertiesHolder();
        }

        public string Topic { get; set; }

        public int Partition { get; set; }

        public string PartitionKey { get; set; }

        public int SequenceNo { get; set; }

        public bool IsPriority { get; set; }

        public string Key { get; set; }

        public long BornTime { get; set; }

        public bool WithHeader { get; set; }

        public ICompletionCallback<SendResult> Callback { get; set; }

        public PropertiesHolder PropertiesHolder { get; set; }

        public T GetBody<T>()
        {
            return (T)body;
        }

        public void AddDurableAppProperty(string name, string value)
        {
            PropertiesHolder.AddDurableAppProperty(name, value);
        }

        public void AddDurableSysProperty(string name, string value)
        {
            PropertiesHolder.AddDurableSysProperty(name, value);
        }

        public string GetDurableAppProperty(string name)
        {
            return PropertiesHolder.GetDurableAppProperty(name);
        }

        public string GetDurableSysProperty(string name)
        {
            return PropertiesHolder.GetDurableSysProperty(name);
        }

        public void AddVolatileProperty(string name, string value)
        {
            PropertiesHolder.AddVolatileProperty(name, value);
        }

        public string GetVolatileProperty(string name)
        {
            return PropertiesHolder.GetVolatileProperty(name);
        }
    }
}
