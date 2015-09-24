using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Producer
{
    public class ChannelBuffer:IChannelBuffer
    {
        private ConcurrentDictionary<string,List<PubMessage>> PublishData =new ConcurrentDictionary<string, List<PubMessage>>();

        public ChannelBuffer()
        {
            
        }

        public void PublishMessage(string identifier, Arch.CMessaging.Core.gen.PubMessage message)
        {
            PublishData.AddOrUpdate(identifier,
                                    k => new List<PubMessage> {message},
                                    (k, list) =>
                                        {
                                            list.Add(message);
                                            return list;
                                        });
          
        }
    }
}
