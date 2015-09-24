using System;
using System.Collections.Generic;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Util;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    /// <summary>
    /// Consumer核心处理类，取数据-发送Ack-获取服务列表
    /// </summary>
    internal sealed class ConsumerBuffer:IConsumerBuffer
    {
        public ConsumerBuffer(ConsumerChannel channel)
        {
            Guard.ArgumentNotNull(channel, channel.GetType().FullName);
            Channel = channel;
        }
        
        private ConsumerChannel Channel { get; set; }

        public string PullingRequestUri { get; set; }
        public int BatchSize { get; set; }
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// 获取批量数据
        /// </summary>
        /// <param name="isPreLoad"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TimeoutException"></exception>
        public List<Message> GetMessages(bool isPreLoad= false)
        {
            Guard.ArgumentNotNullOrEmpty(PullingRequestUri, "PullingRequestUri");
            if (BatchSize < 1) throw new ArgumentException("batchSize is a positive non-zero integer!");

            var chunkSubMessages = Channel.Input.GetChunkSubMessages(PullingRequestUri, BatchSize, ReceiveTimeout, !isPreLoad);
            if (chunkSubMessages == null || chunkSubMessages.Count < 1) return new List<Message>(0);

            var messages = new List<Message>(chunkSubMessages.Count);
            foreach (var chunkSubMessage in chunkSubMessages)
            {
                var message = new Message(chunkSubMessage,new MessageReader(chunkSubMessage)){Consumer = PullingRequestUri};
                //订阅Disposing事件，将消息大小从内存中减去
                message.DestroyMomory+= messageSize => Channel.Input.MomoryManager.AtomicReduce(PullingRequestUri, messageSize);
                messages.Add(message);
            }
            return messages;
        }

        /// <summary>
        /// 向发送队列添加Ack信息
        /// </summary>
        /// <param name="serverHostName">MessageReader.Message.ServerHostName</param>
        /// <param name="ack"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SendAck(string serverHostName, ConsumerAck ack)
        {
            Guard.ArgumentNotNullOrEmpty(serverHostName, "serverHostName");
            Guard.ArgumentNotNull(ack, "ack");
            //根据服务名获取服务URI
            var server = Channel.GetPhysicalServer(serverHostName);
            if (server == null)
            {
                Logg.Write(serverHostName+" 没有对应服务", LogLevel.Warn,"consumer.consumerbuffer.sendack");
                return;
            }
            Channel.Output.AppendAck(PullingRequestUri, server, ack);
        }

        /// <summary>
        /// 获取Servers列表
        /// </summary>
        /// <param name="pullingRequestUri">Consumer pullingRequestUri</param>
        public void GetExchangePhysicalServers(string pullingRequestUri)
        {
            Guard.ArgumentNotNullOrEmpty(pullingRequestUri, "pullingRequestUri");
            Channel.GetExchangePhysicalServers(pullingRequestUri);
        }

        public void Dispose()
        {
            Channel.Input.Remove(PullingRequestUri);
        }
    }
}
