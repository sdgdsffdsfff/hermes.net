using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using cmessaging.consumer.exception;
#if DEBUG
using Arch.CMessaging.Core.ObjectBuilder;
#endif

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class OutputBuffer : IDisposable
    {
        private readonly IService _server;
        //serveruri,consumeruri,ack
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CBox<Tuple<string, ConsumerAck>>>> Output = new ConcurrentDictionary<string, ConcurrentQueue<CBox<Tuple<string, ConsumerAck>>>>();//ACK队列 consumer uri,consumerack 
        private readonly TimerScheduler _timerScheduler;
        private int _ackIntervalTime = Consts.Consumer_AckIntervalTime;
#if DEBUG
        private IDebugLogWriter debugLog;
#endif
        public OutputBuffer(IService server)
        {
            Guard.ArgumentNotNull(server, "server");

            _server = server;
            _timerScheduler = new TimerScheduler();
#if DEBUG
            this.debugLog = ObjectFactory.Current.Get<IDebugLogWriter>(Lifetime.ContainerControlled);
#endif
        }

        /// <summary>
        /// 添加ACK 信息，供CONSUMER使用
        /// </summary>
        /// <param name="consumerUri"> </param>
        /// <param name="server"></param>
        /// <param name="ack"></param>
        public void AppendAck(string consumerUri,PhysicalServer server,ConsumerAck ack)
        {
            Guard.ArgumentNotNull(server, "server");
            Guard.ArgumentNotNull(ack, "ack");

            Output.AddOrUpdate(server.ServerDomainName,
                               s =>
                                   {
                                       var queue = new ConcurrentQueue<CBox<Tuple<string, ConsumerAck>>>();
                                       queue.Enqueue(new CBox<Tuple<string, ConsumerAck>> { Value = new Tuple<string, ConsumerAck>(consumerUri, ack) });
                                       //定时Ack
                                       _timerScheduler.Register(() =>
                                                                    {
                                                                        var tmp = server;
                                                                        RunTaskAck(tmp);
                                                                    }, s, _ackIntervalTime, false);
                                           //对于新增的SERVER 启动TASK 进行自动ACK
                                       return queue;
                                   },
                               (s, d) =>
                                   {
                                       var tmp = server;
                                       d.Enqueue(new CBox<Tuple<string, ConsumerAck>>
                                                     {Value = new Tuple<string, ConsumerAck>(consumerUri, ack)});

                                       if (d.Count >= Consts.Consumer_DefaultAckQueueBoundary) //Queue 数量大于
                                       {
                                           System.Threading.ThreadPool.QueueUserWorkItem(RunTaskAck, tmp);
                                       }
                                       return d;
                                   });
        }
        
        /// <summary>
        /// Task ACK处理
        /// </summary>
        private void RunTaskAck(object obj)
        {
            var server = obj as PhysicalServer;
            if (server == null) return;
            ConcurrentQueue<CBox<Tuple<string, ConsumerAck>>> queue;
            if (!Output.TryGetValue(server.ServerDomainName, out queue)) return;
            var consumerAcks = new List<ConsumerAck>(queue.Count);
            var list = new List<Tuple<string, ConsumerAck>>(queue.Count);
            int size = 0;
            while (size < Consts.Consumer_DefaultAckQueueBoundary)
            {
                size++;
                CBox<Tuple<string, ConsumerAck>> cbox;
                if (!queue.TryDequeue(out cbox)) break;
                var consumerAck = cbox.Value;
                consumerAcks.Add(consumerAck.Item2);
                list.Add(consumerAck);
                cbox.Value = null;
            }
            if (consumerAcks.Count < 1) return;
            //if (queue.Count > 0)
            //{
            //    System.Threading.ThreadPool.QueueUserWorkItem(RunTaskAck, server);
            //}

            for (int i = 0; i < 3; i++)
            {
                try
                {
                   var chunk = new ConsumerAckChunk
                                {
                                    ChunkID = Guid.NewGuid().ToString(),
                                    ConsumerAcks = consumerAcks,
                                    Timestamp = Time.ToTimestamp(),
                                    ClientIP = Local.IPV4
                                };
                
                    var ack = _server.Ack(server, 0, chunk);
                    if (ack != null
                            && (ack.StatusCode == StatusCode.OK
                                || ack.StatusCode == StatusCode.Accepted))
                    {
#if DEBUG
                        foreach (var item in consumerAcks)
                        {
                            debugLog.Write(string.Format("ack->{0},server:{1}",item.MessageID,server.ServerName),item.Uri);
                        }
#endif 
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MetricUtil.Set(new ExceptionCountMetric { HappenedWhere = ExceptionType.OnAck.ToString() });
                    Logg.Write(ex, LogLevel.Error, Consts.Consumer_Title_AckFail, new[]
                                {
                                    new KeyValue{Key = "ServerDomainName",Value = server.ServerDomainName},
                                    new KeyValue{Key = "ServerName",Value = server.ServerName},
                                    new KeyValue{Key = "ServerIP",Value = server.ServerIP}
                                });
                }
                System.Threading.Thread.Sleep(Consts.Consumer_AckRetryIntervalTime);
            }
        }

        public void Dispose()
        {
            var keys = Output.Keys;
            foreach (var key in keys)
            {
                RunTaskAck(key);
            }
        }

        private void ChangeAckIntervalTime(int time)
        {
            _ackIntervalTime = time;
        }
    }
}
