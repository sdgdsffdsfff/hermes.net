using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Impl.Producer.V09;
using Arch.CMessaging.Core.ObjectBuilder;

namespace Arch.CMessaging.Client.Impl.Producer.Check
{
    /*public class ProducerValidator : ValidatorBase
    {
        protected override string Name
        {
            get { return "ProducerValidator"; }
        }

        protected override ValidateResult OnValidate()
        {
            const string exchange = "ExchangeTest";
            const string id = "900205_48db5650";
            const string subject = "exchange.test";
            return SendMessage(exchange, id, subject);
        }

        private ValidateResult SendMessage(string exchange, string id, string subject)
        {
            try
            {
                var producer = ProducerFactory.Instance.Create(exchange, id);
                producer.PublishAsync("test", subject);
            }
            catch (Exception ex)
            {
                return new ValidateResult(ProductStatus.Exception, "Producer Error:" + ex.ToString());
            }
            return new ValidateResult(ProductStatus.Normal, "状态正常");
        }
    }*/
}
