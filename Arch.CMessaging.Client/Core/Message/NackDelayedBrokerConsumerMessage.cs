using System;
using System.Collections.Generic;

namespace Arch.CMessaging.Client.Core.Message
{
    public class NackDelayedBrokerConsumerMessage : IConsumerMessage, PropertiesHolderAware, BaseConsumerMessageAware
    {
        private BrokerConsumerMessage brokerMsg;

        private int resendTimes;

        public NackDelayedBrokerConsumerMessage(BrokerConsumerMessage brokerMsg)
        {
            this.brokerMsg = brokerMsg;
        }

        public void Nack()
        {
            resendTimes++;
            brokerMsg.BaseConsumerMessage.Nack();
        }

        public int ResendTimes
        {
            get
            {
                return resendTimes;
            }
        }

        // below are delegate methods

        public BaseConsumerMessage BaseConsumerMessage
        { 
            get { return brokerMsg.BaseConsumerMessage; } 
        }

        public PropertiesHolder PropertiesHolder
        {
            get{ return brokerMsg.PropertiesHolder; }
        }

        public string GetProperty(string name)
        {
            return brokerMsg.GetProperty(name);
        }

        public IEnumerator<string> GetPropertyNames()
        {
            return brokerMsg.GetPropertyNames();
        }

        public long BornTime{ get { return brokerMsg.BornTime; } }

        public string Topic{ get { return brokerMsg.Topic; } }

        public string RefKey { get { return brokerMsg.RefKey; } }

        public T GetBody<T>()
        {
            return brokerMsg.GetBody<T>();
        }

        public string Status{ get { return brokerMsg.Status; } }

        public void Ack()
        {
            brokerMsg.Ack();
        }
    }
}

