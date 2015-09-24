using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.API
{
    /// <summary>
    /// 消息消费者接口。
    /// <remarks>
    /// 消费者支持同步消费和异步消费。
    /// 在同步模式下，确认也是同步的，意味着，只有消息被正确消费之后，确认信息才会被送达，
    /// 但是请注意，如果同步消费时间过长，超过指定的<see cref="AckTimeout"/>，消息将会被重新分发，即使这条消息可能已经处理成功。
    /// 在异步模式下，可以选择是自动确认，还是同步确认。同步确认发生在消息被正常消费之后。
    /// 自动确认发生在，只要消息被分发到执行线程之后，消息即被确认。
    /// 这种模式的应用通常应该考虑放在，消息的可靠性要不不高，但是又希望尽量减少不必要的重复消费，以及增加消息的处理能力上。
    /// 消息消费者始终应该通过<see cref="IMessageChannel"/>生成，因为<see cref="IMessageChannel"/>维护所有消费者的生命周期以及占用资源。
    /// 强烈建议消费者是个单例。
    /// </remarks>
    /// </summary>
    public interface IMessageConsumer : IDisposable
    {
        /// <summary>
        /// 当有消息的时候，回调才会被触发。
        /// 无论是同步消息处理还是异步消息处理，必须使用此事件处理消息。
        /// </summary>
        event ConsumerCallbackEventHandler Callback;

        /// <summary>
        /// Polling一次Batch的大小，以消息条数记，指消息被确认之前能够缓冲的最大数量。
        /// </summary>
        uint BatchSize { get; set; }

        /// <summary>
        /// 指定接收消息的超时时间，如果在指定超时时间内没有收到消息，将抛出异常，终止本次执行。
        /// </summary>
        uint ReceiveTimeout { get; set; }

        /// <summary>
        /// Consumer身份标识
        /// </summary>
        string Identifier { get; set; }

        /// <summary>
        /// 同步消费，如果绑定的<see cref="IMessageChannel"/>是可靠，或者时序的。
        /// 消息确认只发生在事件回调执行完之后。如果调用该方法，发现没有消息可以接收，
        /// 将会block当前线程，直到下一个消息到了之后，当前线程才会被唤醒，除非设置
        /// ReceiveTimeout，否则调用线程会一致被block。
        /// <example>
        /// var consumer =  ConsumerFactory.Instance.CreateAsTopic＜TopicConsumer＞(topic,exchangeName,identifier);
        /// consumer.Callback += (c, e) =>
        /// {
        ///     try
        ///     {
        ///     }
        ///     catch(Exception)
        ///     {
        ///         e.Message.Acks = AckMode.Nack;
        ///     }
        /// };
        /// consumer.Consume();
        /// 或者由调用线程触发，直接调用 consumer.Consume();
        /// </example>
        /// </summary>
        [Obsolete]
        void Consume();

        /// <summary>
        /// 同步消费，如果绑定的<see cref="IMessageChannel"/>是可靠，或者时序的。
        /// 消息确认只发生在事件回调执行完之后。如果调用该方法，发现没有消息可以接收，
        /// 将会block当前线程至ReceiveTimeout时间结束。
        /// 处理消息时必须使用using。
        /// <example>
        /// var consumer = ConsumerFactory.Instance.CreateAsTopic＜TopicConsumer＞(topic,exchangeName,identifier);
        /// using(var message = consumer.ConsumeOne()){
        ///    try
        ///    {
        ///        if(message!= null){
        ///           //处理逻辑
        ///        }
        ///    }
        ///    catch(Exception)
        ///    {
        ///        message.Acks = AckMode.Nack;
        ///    }
        /// }
        /// </example>
        /// </summary>
        IMessage ConsumeOne();

        /// <summary>
        /// 异步消费，如果绑定的<see cref="IMessageChannel"/>是可靠，或者时序的。
        /// 消息确认只发生在事件回调执行完之后。
        /// <param name="maxThread">
        /// 允许最大同时并行执行的线程数，这个线程是工作线程。
        /// </param>
        /// <param name="autoAck">是否自动确认，默认开启。如果对消息的可靠性要求高，建议关闭，但是消息处理吞吐量将下降。</param>
        /// <example>
        /// var consumer = ConsumerFactory.Instance.CreateAsTopic＜TopicConsumer＞(topic,exchangeName,identifier);
        /// consumer.ConsumeAsync(10); 
        /// 强烈建议上面代码只执行一次。
        /// 或者
        /// 由调用线程触发一次，执行一次，必须指定maxThread = 0;
        /// 当maxThread = 1，本质就是单线程执行，和同步消费是一样的效果。
        /// </example>
        /// </summary>
        void ConsumeAsync(int maxThread = 1, bool autoAck = true);
    }
}
