using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Event;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 描述消息发送通道的接口
    /// </summary>
    /// <remarks>
    /// 消息发送通道用于决定发送的消息是否是可靠的，通过属性参数<see cref="IsReliable"/>辨别，
    /// 或者是否是有时序的，通过属性参数<see cref="IsInOrder"/>辨别。
    /// 消息通道实例一般由<see cref="IMessageChannelFactory"/>生成，并且应该在生成消息通道时指定消息特征，
    /// 默认情况下，消息的可靠性和时序性都得到不到保证，但是有最大的传输吞吐量。
    /// 强烈建议消息通道是单例的。
    /// </remarks>
    /// <example>
    /// var channel = MessageChannelFactory.CreateChannel("http://messaging.global.sh.ctriptravel.com/");
    /// </example>
    public interface IMessageChannel : IDisposable
    {
        /// <summary>
        /// 当消息通道内累计未发送的消息的字节数超过阀值<see cref="Capacity"/>，发出事件通知。
        /// 当接收到此警告的时候，说明已经有多个<see cref="IMessageProducer"/>实例被阻塞调用，或者强制抛出异常。
        /// 监听此事件的方法，应该向日志记录警报，或者向监控发出警报，当多次连续收到此警报，或者已经严重影响系统性能，
        /// 可以通过此事件，强制关闭channel，释放所有占用资源。
        /// </summary>
        event ChannelOutOfCapacityEventHandler OutOfCapacity;

        /// <summary>
        /// 缓存客户端信息，此类信息，一般会经常和服务端做交互。
        /// </summary>
        IDictionary ClientProperties { get; }

        /// <summary>
        /// 缓存服务端信息，此类信息，一般会影响<see cref="IMessageProducer"/>
        /// 以及<see cref="IMessageConsumer"/>的收发策略。
        /// </summary>
        IDictionary ServerProperties { get; }

        /// <summary>
        /// 是否启用监控，如果启用，当前<seealso cref="IMessageChannel"/>产生的所有实例，
        /// 包括自己的状态都会被监控。启用监控会对当前服务产生稍许性能损耗。
        /// </summary>
        bool EnableMonitoring { get; set; }

        /// <summary>
        /// 当前<see cref="IMessageChannel"/>最大允许并发消息连接数，
        /// 用于控制客户端异步发送速度频率，避免过度占用服务器连接资源。
        /// </summary>
        ushort ConnectionMax { get; set; }

        /// <summary>
        /// 指定确认超时事件，这个属性只在可靠消息模式，或者时序消息模式下起作用。当消息确认超时，
        /// <see cref="IMessageProducer"/>会发出警报，此时由调用方选择，重新发送，或者放弃。
        /// </summary>
        int AckTimeout { get; set; }

        /// <summary>
        /// 指定<see cref="IMessageChannel"/>最大容量，以字节记。
        /// </summary>
        uint Capacity { get; set; }

        /// <summary>
        /// 当前已经占用容量，以字节记。
        /// </summary>
        uint CurrentSize { get; }

        /// <summary>
        /// 是否可靠消息传输
        /// </summary>
        bool IsReliable { get; }

        /// <summary>
        /// 是否时序消息传输，在当前版本中，时序保证是通过时序号由客户端自己保证的。
        /// </summary>
        bool IsInOrder { get; }

        /// <summary>
        /// 目标消息服务地址，CMessaging目前是基于Http协议开发的。
        /// </summary>
        string Uri { get; }

        /// <summary>
        /// 已知的消息服务器地址，缓存了消息服务器的IP，以及其他描述性属性。
        /// </summary>
        string[] KnownHosts { get; }

        /// <summary>
        /// 通道时否已经打开。
        /// </summary>
        bool IsOpen { get; } 

        /// <summary>
        /// 在第一次使用消息通道的时候，必须调用一次，以完成初始化工作，并缓存服务端一些告知的信息。
        /// </summary>
        /// <param name="timeout">调用超时，调用超时后会抛出异常，
        /// 注意此方法在没有try catch的静态方法下使用会造成类初始化失败</param>
        void Open(int timeout);

        /// <summary>
        /// 关闭消息通道，此时，如果有连接正在发送消息，会等待<paramref name="timeout"/>个时间，
        /// 如果超时时间已过，还没有请求确认或应答，强制关闭连接，并释放资源。
        /// </summary>
        /// <param name="timeout">关闭超时时间</param>
        void Close(int timeout);
    }
}
