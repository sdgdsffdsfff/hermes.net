using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.API
{
    public interface IMessage : IDisposable
    {
        /// <summary>
        /// <seealso cref="IHeaderProperties"/>
        /// </summary>
        IHeaderProperties HeaderProperties { get; }

        /// <summary>
        /// 以文本形式获取消息，如果消息体本身不是文本类型，或者消息不存在，返回空。
        /// </summary>
        /// <returns>消息文本</returns>
        string GetText();
        /// <summary>
        /// 已二进制形式获取消息，消息体不存在，将返回空。
        /// </summary>
        /// <returns>二进制</returns>
        byte[] GetBinary();

        /// <summary>
        /// 以指定对象类型返回可反序列化对象。如果反序列化失败，将抛出异常。
        /// 如果需要返回的基础类型无法转换，将抛出异常。
        /// 如果消息不存在，将返回默认值。
        /// </summary>
        /// <typeparam name="TObject">对象类型</typeparam>
        /// <returns>对象</returns>
        TObject GetObject<TObject>();

        /// <summary>
        /// 以流形式获取消息，消息不存在，将返回空。
        /// </summary>
        /// <returns>消息流</returns>
        Stream GetStream();

        /// <summary>
        /// 是否确认消息，当处理中有异常，请确认方法最后执行AckMode.Nack，
        /// 否则消息将执行确认操作。
        /// <example>
        /// try
        /// {
        /// }
        /// catch(Exception)
        /// {
        ///     Acks = AckMode.Nack;
        /// }
        /// </example>
        /// </summary>
        AckMode Acks { get; set; }
    }
}
