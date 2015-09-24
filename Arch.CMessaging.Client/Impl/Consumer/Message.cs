using System;
using System.IO;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Util;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Log;
using cmessaging.consumer;
using cmessaging.consumer.exception;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    public class Message:IMessage
    {
        internal ChunkSubMessage ChunkSubMessage { get; private set; }
        internal DateTime BeginTime { get; set; }
        internal MessageReader Reader { get; private set; }
        internal bool AutoAck { get; set; }
        internal string Consumer { get; set; }
        public Message(ChunkSubMessage message, MessageReader messageReader)
        {
            this.ChunkSubMessage = message;
            this.Reader = messageReader;
            Acks= AckMode.Ack;
            BeginTime = Time.Now();
        }

        private IHeaderProperties _headerProperties;
        /// <summary>
        /// <seealso cref="IHeaderProperties"/>
        /// </summary>
        public IHeaderProperties HeaderProperties
        {
            get { return _headerProperties ?? (_headerProperties = Reader.HeaderProperties); }
        }

        private string _text;
        /// <summary>
        /// 以文本形式获取消息，如果消息体本身不是文本类型，或者消息不存在，返回空。
        /// </summary>
        /// <returns>消息文本</returns>
        public string GetText()
        {
            try
            {
                if (string.IsNullOrEmpty(_text))
                {
                    _text = Reader.GetText();
                }
                return _text;
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.message", new KeyValue {Key = "consumer", Value = Consumer});
                MetricUtil.Set(new ExceptionCountMetric { Consumer = Consumer, HappenedWhere = ExceptionType.OnMessageRead.ToString() });
                throw ex;
            }
        }

        private byte[] _bytes;
        /// <summary>
        /// 已二进制形式获取消息，消息体不存在，将返回空。
        /// </summary>
        /// <returns>二进制</returns>
        public byte[] GetBinary()
        {
            try
            {
                return _bytes ?? (_bytes = Reader.GetBinary());
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.message", new KeyValue { Key = "consumer", Value = Consumer });
                MetricUtil.Set(new ExceptionCountMetric { Consumer = Consumer, HappenedWhere = ExceptionType.OnMessageRead.ToString() });
                throw ex;
            }
        }

        private object _object;
        /// <summary>
        /// 以指定对象类型返回可反序列化对象。如果反序列化失败，将抛出异常。
        /// 如果需要返回的基础类型无法转换，将抛出异常。
        /// 如果消息不存在，将返回默认值。
        /// </summary>
        /// <typeparam name="TObject">对象类型</typeparam>
        /// <returns>对象</returns>
        public TObject GetObject<TObject>()
        {
            try
            {
                if (_object == null || !(_object is TObject))
                {
                    var obj = Reader.GetObject<TObject>();
                    _object = obj;
                }
                return (TObject)_object;
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.message", new KeyValue { Key = "consumer", Value = Consumer });
                MetricUtil.Set(new ExceptionCountMetric { Consumer = Consumer, HappenedWhere = ExceptionType.OnMessageRead.ToString() });
                throw ex;
            }
        }


        private Stream _stream;
        /// <summary>
        /// 以流形式获取消息，消息不存在，将返回空。
        /// </summary>
        /// <returns>消息流</returns>
        public Stream GetStream()
        {
            try
            {
                return _stream ?? (_stream = Reader.GetStream());
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.message", new KeyValue { Key = "consumer", Value = Consumer });
                MetricUtil.Set(new ExceptionCountMetric { Consumer = Consumer, HappenedWhere = ExceptionType.OnMessageRead.ToString() });
                throw ex;
            }
        }

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
        public Arch.CMessaging.Core.Content.AckMode Acks { get; set; }

        public delegate void DisposingHandler(Message message);
        public DisposingHandler Disposing;

        internal delegate void DestroyMomoryHandler(int messageSize);
        internal DestroyMomoryHandler DestroyMomory;
        ThreadSafe.Integer countor = new ThreadSafe.Integer(0);
        public void Dispose()
        {
            try
            {
                if (countor.AtomicIncrementAndGet() > 1) return;//只允许执行一次DISPOSE
                if (Disposing != null)
                {
                    Disposing(this);
                }
                if (DestroyMomory != null)
                {
                    int size = ChunkSubMessage.Pub == null ? 0 : ChunkSubMessage.Pub.Size;
                    DestroyMomory(size);
                }
            }
            catch (Exception exception)
            {
                Logg.Write(exception,LogLevel.Error, "cmessaging.consumer.message.dispose");
            }
        }
    }
}
