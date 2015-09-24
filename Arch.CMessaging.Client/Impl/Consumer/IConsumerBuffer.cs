using System;
using System.Collections.Generic;
using Arch.CMessaging.Core.gen;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public interface IConsumerBuffer:IDisposable
    {
        string PullingRequestUri { get; set; }
        int BatchSize { get; set; }
        int ReceiveTimeout { get; set; }
        /// <summary>
        /// 获取批量数据
        /// </summary>
        /// <param name="isPreLoad"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="TimeoutException"></exception>
        List<Message> GetMessages(bool isPreLoad = false);
        /// <summary>
        /// 向发送队列添加Ack信息
        /// </summary>
        /// <param name="serverHostName">MessageReader.Message.ServerHostName</param>
        /// <param name="ack"></param>
        /// <exception cref="ArgumentException"></exception>
        void SendAck(string serverHostName,ConsumerAck ack);

        /// <summary>
        /// 获取ExchangePhysicalServer列表
        /// </summary>
        /// <param name="identifier">Consumer pullingRequestUri</param>
        void GetExchangePhysicalServers(string identifier);
    }
}
