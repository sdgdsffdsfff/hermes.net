using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 消息生产者接口。
    /// <remarks>
    /// 在CMessaging设计当中，消息生产者发送的消息永远是异步的。
    /// 在.net 2.0版本中，异步消息发送线程本质上还是一个同步阻塞线程，事件回调发生在工作线程中，所以调用者应注意对回调响应的控制。
    /// 在.net 4.0版本及以上版本，发送消息的是个非阻塞线程，事件回调发生在I/O线程中，因此能得到更好的性能。
    /// 消息生产者始终应该通过<see cref="IMessageChannel"/>生成，因为<see cref="IMessageChannel"/>维护所有生产者的生命周期以及占用资源。
    /// </remarks>
    /// <example>
    /// var channel = MessageChannelFactory.CreateChannel("http://messaging.global.sh.ctriptravel.com/");
    /// var producer = channel.CreateProducer();
    /// </example>
    /// </summary>
    public interface IMessageProducer : IDisposable
    {
        /// <summary>
        /// 当Broker确认消息成功后触发，触发此事件，证明消息已经在服务端持久化，消息本身是可靠的。
        /// 确认回调只在可靠传输模式以及时序传输模式下有效。
        /// </summary>
        event BrokerAckEventHandler BrokerAcks;

        /// <summary>
        /// 当Broker拒绝消息后触发，触发此事件，一般是因为Broker内部产生错误，或者缓冲区已满的时候。
        /// 通过<see cref="BrokerNackEventArgs"/>可以指定消息是否重新发送，如果指定重新发送，
        /// 消息不会立即发送，而是进入Retry的列表，按退避算法后发送，如果3次重发，Broker还是拒绝，
        /// 此事件还会再次被触发，此时调用者应该决定还有没有必要发送，如果还是重新发送，继续退避，
        /// 直到到达发送时间，注意这个过程可能很长，对象本身会始终占用内存。
        /// </summary>
        event BrokerNackEventHandler BrokerNacks;

        /// <summary>
        /// 当发送消息的过程当中发生错误的时候触发，同<see cref="BrokerNacks"/>一样，调用者
        /// 可以选择是否重发，过程和<see cref="BrokerNacks"/>一样。
        /// </summary>
        event CallbackExceptionEventHandler CallbackException;

        /// <summary>
        /// 当流控开始的时候触发，调用者通过此事件，可以适当控制一下上游线程发送消息的速度，
        /// 如果不对发送消息进行控制，最终可能会引发<seealso cref="ChannelOutOfCapacityEventHandler"/>事件。
        /// </summary>
        event FlowControlEventHandler FlowControl;

        /// <summary>
        /// 滑动窗口大小，按消息条数记，指明在Broker确认消息之前，最多可缓冲的已发送，但未确认的消息。
        /// 左窗口滑动是窗口收拢的过程，表明缓冲窗口正在变小，右窗口滑动，是窗口张开的过程，表明缓冲窗口正在变大。
        /// </summary>
        uint WindowSize { get; set; }

        /// <summary>
        /// 是否启用流控，启用流控，会降低消息吞吐量，但是能够控制不必要的网络带宽。
        /// 这个设置只对滑动窗口启作用，高低水位的流控，不受这个参数影响。
        /// </summary>
        bool UseFlowControl { get; set; }

        /// <summary>
        /// 是否正处于流控中，监控有效。
        /// </summary>
        bool IsUnderFlowControl { get; }

        /// <summary>
        /// Producer的身份标识
        /// </summary>
        string Identifier { get; set; }

        /// <summary>
        /// 申明这个消息生产者使用哪种Exchange。
        /// Exchange实例由Exchange服务维护，每个Exchange原则是只绑定一个消息管道，只对一个消息管道投递消息。
        /// 但当一个Exchange绑定多个消息管道的时候，说明此Exchange是支持Sharding的。
        /// 每个Producer只能申明一个Exchange，重复申明将抛出异常。
        /// 如果需要使用不同的Exchange，可以通过<see cref="IMessageChannel"/>生成一个新的消息生产者。
        /// </summary>
        /// <param name="exchange">Exchange实例名</param>
        void ExchangeDeclare(string exchangeName);

        /// <summary>
        /// 向订阅者以非阻塞的方式发布一个消息。
        /// </summary>
        /// <typeparam name="TMessage">消息的类型，应该是一个可以被序列化的类型</typeparam>
        /// <param name="message">消息内容，可以是任何可以被序列化的对象，也可以是一个已经被序列化后的二进制数组，序列化方式可以在<see cref="SerializationType"/>申明</param>
        /// <param name="subject">消息的主题，订阅者必须根据这个消息主题进行订阅。格式应该是: {0}.{1}.{2}，点号数量不限。
        /// 如果使用其他格式，无法保证Topic Fuzzy Matching能够匹配，只能使用Direct模式。</param>
        /// <param name="header"><see cref="MessageHeader"/>消息头</param>
        void PublishAsync<TMessage>(TMessage message, string subject, MessageHeader header = null);
    }
}
