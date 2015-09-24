using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using cmessaging.consumer.exception;
using cmessaging.consumer.handling;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;
using cmessaging.consumer.message;
#if DEBUG
using Arch.CMessaging.Core.ObjectBuilder;

#endif

namespace Arch.CMessaging.Client.Impl.Consumer
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
    public abstract class AbstractConsumer : IMessageConsumer
    {
        private readonly CancellationTokenSource _cts;
        private readonly ThreadPool _threadPool;
        private ThreadSafe.Boolean _isTaskStart;
#if DEBUG
        private IDebugLogWriter debugLog;
#endif
        protected AbstractConsumer(IConsumerBuffer buffer)
        {
            Guard.ArgumentNotNull(buffer, buffer.GetType().FullName);
            Buffer = buffer;

            _cts = new CancellationTokenSource();
            _threadPool = new ThreadPool();
            _isTaskStart = new ThreadSafe.Boolean(false);
            
            ConfigUtil.Instance.NotifyPropertyChange += OnNotifyPropertyChange;
#if DEBUG
            this.debugLog = ObjectFactory.Current.Get<IDebugLogWriter>(Lifetime.ContainerControlled);
#endif 
        }

        private ThreadSafe.Boolean isRegisterCallback= new ThreadSafe.Boolean(false);
        private event ConsumerCallbackEventHandler callback;
        /// <summary>
        /// 当有消息的时候，回调才会被触发。
        /// 无论是同步消息处理Consume还是异步消息ConsumeAsync处理，必须使用此事件处理消息。
        /// 此事件只对第一次注册有效，后续注册无效
        ///  </summary>
        public event ConsumerCallbackEventHandler Callback
        {
            add
            {
                if(!isRegisterCallback.ReadFullFence())
                {
                    callback += value;
                    isRegisterCallback.AtomicExchange(true);
                }
            }
            remove
            {
                if(isRegisterCallback.ReadFullFence())
                {
                    callback -= value;
                    isRegisterCallback.AtomicExchange(false);
                }
            }
        }

        /// <summary>
        /// ConsumeAsync方法中发生异常才会被触发 
        /// </summary>
        public event ConsumeExceptionEventHandler ConsumeAsyncException;
        public event CallbackExceptionEventHandler CallbackException;

        #region 属性

        private bool _setBatchSize = false;
        private uint _batchSize;
        /// <summary>
        /// Polling一次Batch的大小，以消息条数记，指消息被确认之前能够缓冲的最大数量。
        /// </summary>
        public uint BatchSize
        {
            get
            {
                if(_batchSize <1)
                {
                    setBatchSize(ConfigUtil.Instance.GetBatchSize(ConfigKey));
                }
                return _batchSize;
            }
            set
            {
                if (value < 1 || _batchSize == value) return;
                _setBatchSize = true;
                setBatchSize(value);
            }
        }
        private void setBatchSize(uint value)
        {
            _batchSize = value;
            Buffer.BatchSize = (int)value; //更新Buffer.batchsize
        }

        private bool _setReceiveTimeout = false;
        private uint _receiveTimeout;
        /// <summary>
        /// 指定接收消息的超时时间，如果在指定超时时间内没有收到消息，将抛出异常，终止本次执行。
        /// </summary>
        public uint ReceiveTimeout
        {
            get
            {
                if(_receiveTimeout<1)
                {
                    setReceiveTimeout(ConfigUtil.Instance.GetReceiveTimeout(ConfigKey));
                }
                return _receiveTimeout;
            }
            set
            {
                if (value < 200)
                {
                    _receiveTimeout = 200;
                    return;
                }
                if (_receiveTimeout == value) return;
                _setReceiveTimeout = true;
                setReceiveTimeout(value);
            }
        }
        private void setReceiveTimeout(uint value)
        {
            _receiveTimeout = value;
            Buffer.ReceiveTimeout = (int)value;
        }

        /// <summary>
        /// Consumer身份标识
        /// </summary>
        public string Identifier { get; set; }

        protected abstract string PullingRequestUri { get; }
        
        protected IConsumerBuffer Buffer { get; set; }

        /// <summary>
        /// ConsumerAsync 中Task 是否已启动
        /// </summary>
        private bool IsTaskStart
        {
            get { return !(_isTaskStart.AtomicCompareExchange(true, false)); }
        }

        private DateTime heartBeatTime = DateTime.MaxValue;

        public bool IsDispose
        {
            get { var iscancell = _cts.IsCancellationRequested;
                var checktime =heartBeatTime.AddMilliseconds(Consts.Consumer_AsyncQueueDequeueTimeout * 2) < DateTime.Now;
                if (!(iscancell || checktime))
                {
                    Logger.Write(string.Format("dispose, iscancell:{0},heartbeattime:{1}", iscancell, heartBeatTime.ToString("yyyy-MM-dd hh:mm:ss fff")), LogLevel.Warn, "IsDispose", new[] { new KeyValue { Key = "Consumer", Value = PullingRequestUri } });
                }
                return iscancell || checktime;
            }
        }
        protected abstract string ExchangeName { get; }
        protected string ConfigKey { get { return string.Format("{0}_{1}", ExchangeName, Identifier); } }
        #endregion

        /// <summary>
        /// 同步消费，如果绑定的<see cref="IMessageChannel"/>是可靠，或者时序的。
        /// 消息确认只发生在事件回调执行完之后。如果调用该方法，发现没有消息可以接收，
        /// 将会block当前线程至ReceiveTimeout时间结束。
        /// <example>
        /// var consumer = ConsumerFactory.Instance.CreateAsTopic＜TopicConsumer＞(topic,exchangeName,identifier);
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
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        [Obsolete]
        public void Consume()
        {
            if (string.IsNullOrEmpty(PullingRequestUri)) throw new InvalidOperationException("Please call the bind method!");

            try
            {
                Logger.Write("Consume is run", LogLevel.Debug, "Consume", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });

                Buffer.GetExchangePhysicalServers(PullingRequestUri);
                var messages = Buffer.GetMessages();
                if (messages == null || messages.Count < 1)
                {
                    Logg.Write("No Messages",LogLevel.Info,"Consumer.Consume",new[]{new KeyValue{Key="Consumer",Value = PullingRequestUri} } );
                    return;
                }
                foreach (var message in messages)
                {
                    message.AutoAck = false;
                    ProcessMessage(message);
                }
            }
            catch (Exception exception)
            {
                MetricUtil.Set(new ExceptionCountMetric { HappenedWhere = ExceptionType.OnMessageHandle.ToString(), Consumer = PullingRequestUri });
                Logg.Write(exception, LogLevel.Error, Consts.Consumer_Title_ConsumeException, new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri });
            }
        }

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
        public IMessage ConsumeOne()
        {
            if (string.IsNullOrEmpty(PullingRequestUri)) throw new InvalidOperationException("Please call the bind method!");

            try
            {
                Logger.Write("consumerone is run", LogLevel.Debug, "ConsumeOne", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });

                Buffer.GetExchangePhysicalServers(PullingRequestUri);//获取服务地址
                var messages = Buffer.GetMessages();//获取消息Reader
                if (messages == null || messages.Count < 1)
                {
                    Logg.Write("No Messages", LogLevel.Info, "consumer.consumerone", new[] { new KeyValue { Key = "Consumer", Value = PullingRequestUri } });
                    return null;
                }
                var message = messages[0];
                message.Disposing += MessageDisposing;
                var messagid = (message.ChunkSubMessage.Pub != null) ? message.ChunkSubMessage.Pub.MessageID : "";
                var correlationID = message.HeaderProperties == null ? "" : message.HeaderProperties.CorrelationID;
                Logg.Write(string.Format("consumer send messageid:{0}", messagid), LogLevel.Info, "consumer.callbackbegin", new[]
                {
                    new KeyValue {Key = "PullingRequestUri", Value = PullingRequestUri},
                    new KeyValue {Key = "MessageID", Value =messagid},
                    new KeyValue{Key="CorrelationID",Value=correlationID}
                });
                return message;
            }
            catch (Exception exception)
            {
                MetricUtil.Set(new ExceptionCountMetric { HappenedWhere = ExceptionType.OnMessageHandle.ToString(), Consumer = PullingRequestUri });
                Logg.Write(exception, LogLevel.Error, Consts.Consumer_Title_ConsumeException, new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri });
            }
            return null;
        }

        /// <summary>
        /// 异步消费，如果绑定的<see cref="IMessageChannel"/>是可靠，或者时序的。
        /// 消息确认只发生在事件回调执行完之后。
        /// <example>
        /// var consumer = ConsumerFactory.Instance.CreateAsTopic＜TopicConsumer＞(topic,exchangeName,identifier);
        /// consumer.ConsumeAsync(10); 
        /// 强烈建议上面代码只执行一次，只有第一次执行才有效
        /// 或者
        /// 由调用线程触发一次，执行一次，必须指定maxThread = 0;
        /// </example>
        /// <param name="maxThread">
        /// 允许最大同时并行执行的线程数，这个线程是工作线程。
        /// </param>
        /// <param name="autoAck">是否自动确认，默认开启。如果对消息的可靠性要求高，建议关闭，但是消息处理吞吐量将下降。</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// </summary>
        public void ConsumeAsync(int maxThread, bool autoAck = true)
        {
            if (string.IsNullOrEmpty(PullingRequestUri)) throw new InvalidOperationException("Please call the bind method!");

            if (IsTaskStart) return;

            var poolSize = ConfigUtil.Instance.GetPoolSize(ConfigKey);
            if (poolSize > 0)
            {
                _threadPool.ChangeMaxPoolSize(poolSize);
                ConnectionLimitManager.SetConnection(PullingRequestUri, poolSize);
            }
            else
            {
                _threadPool.MaxPoolSize = maxThread;
                ConnectionLimitManager.SetConnection(PullingRequestUri, maxThread);
            }

            Buffer.GetExchangePhysicalServers(PullingRequestUri);
            new Task(() =>
            {
                var ack = autoAck;
                while (true)
                {
                    try
                    {
                        heartBeatTime = DateTime.Now; //用于检测CONSUMER是否活着
                        Logger.Write("consumer is get message", LogLevel.Debug, "consumeasync", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });
                        //线程取消
                        if (_cts.Token.IsCancellationRequested)
                        {
                            Logger.Write("task is cancel", LogLevel.Warn,"consumeasync", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });
                            _isTaskStart.AtomicExchange(false);
                            return;
                        }
                        var messages = Buffer.GetMessages(true);
#if DEBUG
                        debugLog.Write(string.Format("TryGetMessages->{0}", messages == null ? 0 : messages.Count), PullingRequestUri);
#endif
                        if (messages == null || messages.Count < 1) continue;
                        var i = 0;
                        while (i < messages.Count)
                        {
                            if (_cts.Token.IsCancellationRequested) break;//跳出当前WHILE
                            var message = messages[i];
                            try
                            {
                                var thread = _threadPool.Acquire(); //分发线程控制
                                message.AutoAck = ack;
                                thread.Run(() => ProcessMessage(message)); //执行分发
                                if (ack)
                                {
                                    message.Acks = AckMode.Ack;
                                    SendAck(0, message);
                                }
                                i++;
                            }
                            catch (TimeoutException)
                            {
                                Logger.Write("acquire thread is timeout", LogLevel.Debug, "consumeasync", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });
                            }
                            catch(Exception ex)
                            {
                                MetricUtil.Set(new ExceptionCountMetric
                                {
                                    HappenedWhere = ExceptionType.OnMessageHandle.ToString(),
                                    Consumer = PullingRequestUri
                                });

                                Logg.Write(ex, LogLevel.Error,
                                             Consts.Consumer_Title_ConsumeAsync,
                                             new KeyValue
                                             {
                                                 Key = "PullingRequestUri",
                                                 Value = PullingRequestUri
                                             });
                                if (ConsumeAsyncException != null) ConsumeAsyncException(this, new ConsumeExceptionEventArgs(ex, this)); 
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        MetricUtil.Set(new ExceptionCountMetric
                                                                    {
                                                                        HappenedWhere = ExceptionType.OnMessageHandle.ToString(),
                                                                        Consumer = PullingRequestUri
                                                                    });

                        Logg.Write(ex, LogLevel.Error,
                                     Consts.Consumer_Title_ConsumeAsync,
                                     new KeyValue
                                         {
                                             Key = "PullingRequestUri",
                                             Value = PullingRequestUri
                                         });
                        if (ConsumeAsyncException != null) ConsumeAsyncException(this, new ConsumeExceptionEventArgs(ex, this));
                    }
                }
            }, _cts.Token, TaskCreationOptions.LongRunning).Start();
        }

        /// <summary>
        /// 对Reader 进行处理，记录消息处理时间及Callback消息
        /// </summary>
        /// <param name="message"></param>
        private void ProcessMessage(Message message)
        {
            var messagid = (message.ChunkSubMessage != null && message.ChunkSubMessage.Pub != null) ? message.ChunkSubMessage.Pub.MessageID : "";
            try
            {
                var correlationID = message.HeaderProperties == null ? "" : message.HeaderProperties.CorrelationID;
                message.BeginTime = Time.Now();
                Logg.Write(string.Format("Consumer callback begin,messageid:{0}", messagid), LogLevel.Info, Consts.Consumer_Title_CallbackBegin, new[]
                {
                    new KeyValue {Key = "PullingRequestUri", Value = PullingRequestUri},
                    new KeyValue {Key = "MessageID", Value =messagid},
                    new KeyValue{Key="CorrelationID",Value=correlationID},
                    new KeyValue{Key = "timelatency",Value =(Time.ToTimestamp(message.BeginTime) - message.HeaderProperties.Timestamp).ToString() }, 
                });
                message.Disposing += MessageDisposing;
                MetricUtil.Set(new MessageLatencyMetric { Consumer = PullingRequestUri }, Time.ToTimestamp(message.BeginTime) - message.HeaderProperties.Timestamp);
#if DEBUG
                debugLog.Write(string.Format("processmessage->{0},server:{1}", messagid, message.ChunkSubMessage.ServerHostName), PullingRequestUri);
#endif

                using (message)
                {
                    if (callback != null)
                    {
                        try
                        {
                            var args = new ConsumerCallbackEventArgs(message.Reader) { Message = message, Acks = AckMode.Ack };
                            callback(this, args);
                            message.Acks = args.Message.Acks;
                        }
                        catch (Exception exception)
                        {
                            message.Acks = AckMode.Nack;
                            Logg.Write(exception, LogLevel.Error, Consts.Consumer_Title_ConsumeException, new[]
                                                                {
                                                                    new KeyValue {Key = "PullingRequestUri", Value = PullingRequestUri},
                                                                    new KeyValue {Key = "MessageID", Value = messagid},
                                                                    new KeyValue{Key="CorrelationID",Value=correlationID}
                                                                });

                            MetricUtil.Set(new ExceptionCountMetric()
                            {
                                HappenedWhere = ExceptionType.OnConsuming.ToString(),
                                Consumer = PullingRequestUri
                            });

                            if (CallbackException != null) CallbackException(this, new CallbackExceptionEventArgs(exception, message.Reader));
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Write(ex, LogLevel.Error, "processmessage", new[]
                                                                {
                                                                    new KeyValue {Key = "Consumer", Value = PullingRequestUri},
                                                                    new KeyValue {Key = "messageid", Value =messagid}
                                                                });
            }
        }

        private void MessageDisposing(Message message)
        {
            try
            {
                var milliseconds = (Time.Now() - message.BeginTime).TotalMilliseconds;
                MetricUtil.Set(new HandlingCountMetric { Consumer = PullingRequestUri, Latency = milliseconds, MessageLatency = Time.ToTimestamp(message.BeginTime) - message.HeaderProperties.Timestamp });
                MetricUtil.Set(new HandlingLatencyMetric { Consumer = PullingRequestUri }, milliseconds);

                if (!message.AutoAck) SendAck((int) milliseconds,message);

                var messagid = (message.ChunkSubMessage.Pub != null) ? message.ChunkSubMessage.Pub.MessageID : "";
                var correlationID = message.HeaderProperties == null ? "" : message.HeaderProperties.CorrelationID;
                Logg.Write(string.Format("consumer process messageid:{0} , AckMode:{1} end", messagid,message.Acks), LogLevel.Info, "consumer.callbackend", new[]
                {
                    new KeyValue {Key = "PullingRequestUri", Value = PullingRequestUri},
                    new KeyValue {Key = "MessageID", Value = messagid},
                    new KeyValue{Key="CorrelationID",Value=correlationID}
                });
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.abstractconsumer.messagedisposing");
            }
        }

        /// <summary>
        /// 发送Ack消息
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="message"></param>
        private void SendAck(int duration,Message message)
        {
            var ack = new ConsumerAck
                          {
                              Duration = duration,
                              MessageID = message.ChunkSubMessage.Pub.MessageID,
                              Position = message.ChunkSubMessage.Position,
                              Timestamp = message.ChunkSubMessage.Timestamp,
                              Acks = message.Acks, 
                              Uri = PullingRequestUri
                          };
            Buffer.SendAck(message.ChunkSubMessage.ServerHostName, ack);
        }
        /// <summary>
        /// 通知 batchsize,receivetimeout修改
        /// </summary>
        /// <param name="property"></param>
        public void OnNotifyPropertyChange(NotifyProperty property)
        {
            switch (property)
            {
                case NotifyProperty.BatchSize:
                    if (_setBatchSize) break;
                    setBatchSize(ConfigUtil.Instance.GetBatchSize(ConfigKey));
                    break;
                case NotifyProperty.ReceiveTimeout:
                    if (_setReceiveTimeout) break;
                    setReceiveTimeout(ConfigUtil.Instance.GetReceiveTimeout(ConfigKey));
                    break;
                case NotifyProperty.PoolSize:
                    var poolSize = ConfigUtil.Instance.GetPoolSize(ConfigKey);
                    if (poolSize > 0)
                    {
                        _threadPool.ChangeMaxPoolSize(poolSize);
                        ConnectionLimitManager.SetConnection(PullingRequestUri, poolSize);
                    }
                    break;
                default:
                    break;
            }
        }


        public void Dispose()
        {
            Logger.Write("consumer is dispose!", LogLevel.Warn, "Dispose", new[] { new KeyValue { Key = "PullingRequestUri", Value = PullingRequestUri } });

            _cts.Cancel();
            ConnectionLimitManager.DisposeConnection(PullingRequestUri);
        }
    }
}
