using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.Util;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;
using cmessaging.consumer;
using cmessaging.consumer.pulling.discard;
using System.IO;
#if DEBUG
using Arch.CMessaging.Core.ObjectBuilder;
#endif

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class QueueManager : IDisposable
    {
        //消息队列 consumer uri,queue
        private readonly ConcurrentDictionary<string, ConcurrentQueue<CBox<ChunkSubMessage>>> _syncQueue = new ConcurrentDictionary<string, ConcurrentQueue<CBox<ChunkSubMessage>>>();
        private readonly ConcurrentDictionary<string, BlockingCollection<CBox<ChunkSubMessage>>> _asyncQueue = new ConcurrentDictionary<string, BlockingCollection<CBox<ChunkSubMessage>>>();
        private readonly ConcurrentDictionary<string, long> _consumerTask = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<long, CancellationTokenSource> _cancellationTokenDict = new ConcurrentDictionary<long, CancellationTokenSource>();
        private readonly ConcurrentDictionary<string, BlockingCollection<int>> _pullingBlocking = new ConcurrentDictionary<string, BlockingCollection<int>>();
        private ThreadSafe.Long _id = new ThreadSafe.Long(0);

#if DEBUG
        private IDebugLogWriter debugLog;
#endif

        public QueueManager()
        {
            MomoryManager = new MemoryManager();
#if DEBUG
            this.debugLog = ObjectFactory.Current.Get<IDebugLogWriter>(Lifetime.ContainerControlled);
#endif
        }
        public MemoryManager MomoryManager { get; set; }

        public List<ChunkSubMessage> Take(string uri, int batchSize, Func<string, int, SubChunk> func)
        {
            var queue = _syncQueue.GetOrAdd(uri, s => new ConcurrentQueue<CBox<ChunkSubMessage>>());
            CBox<ChunkSubMessage> message;
            if (!queue.TryDequeue(out message))
            {
                #region 取数据
                try
                {
                    var chunk = func(uri, batchSize);
                    if (chunk != null && chunk.Messages != null)
                    {
                        var total = chunk.Messages.Count;
                        for (var i = 0; i < total; i++)
                        {
                            var subMessage = chunk.Messages[i];
                            if (subMessage.Pub == null)
                            {
                                MetricUtil.Set(new PullingDiscardCountMetric { Consumer = uri }); //记录丢弃的消息数
                                continue;
                            }
                            if (!MomoryManager.AtomicAdd(uri, subMessage.Pub.Size))
                            {
                                MetricUtil.Set(new PullingDiscardCountMetric { Consumer = uri }, (total - i)); //记录丢弃的消息数
                                break;
                            }
                            queue.Enqueue(new CBox<ChunkSubMessage>
                            {
                                Value = new ChunkSubMessage
                                {
                                    Position = subMessage.Position,
                                    Pub = subMessage.Pub,
                                    ServerHostName = chunk.ServerName,
                                    Timestamp = chunk.Timestamp
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logg.Write(ex, LogLevel.Error, "consumer.consumerqueue.syncdequeue", new[] { new KeyValue { Key = "uri", Value = uri } });
                }
                #endregion
                queue.TryDequeue(out message);
            }
            if (message == null) return null;
            var temp = message.Value;
            message.Value = null;
            return new List<ChunkSubMessage> { temp };
        }

        public List<ChunkSubMessage> TakeAsync(string consumer, int batchSize, Func<string, int, CancellationToken, SubChunk> func)
        {
            var queue = _asyncQueue.GetOrAdd(consumer, s => new BlockingCollection<CBox<ChunkSubMessage>>());
            var pullingBlocking = _pullingBlocking.GetOrAdd(consumer, new BlockingCollection<int>());
            long value;
            if (!_consumerTask.TryGetValue(consumer, out value))
            {
                var currentId = _id.AtomicIncrementAndGet();
                //if (currentId > (long.MaxValue - 1000)) _id.AtomicExchange(0);//还原
                if (_consumerTask.TryAdd(consumer, currentId))
                {
                    if (_cancellationTokenDict.TryAdd(currentId, new CancellationTokenSource()))
                        Task(consumer, batchSize, func);//起线程
                }
            }
            var percent = (Consts.Consumer_Percent + 0.1);
            if ((queue.Count / (float)batchSize) < percent) pullingBlocking.Add(1); //激发拉数据

            //从队列中取数据
            var size = (int)(batchSize * Consts.Consumer_Percent);
            size = (queue.Count < size) ? queue.Count : size;
            var list = new List<ChunkSubMessage>(size);
            for (var i = 0; i < size; i++)
            {
                if (!TakeBlocking(queue, list, 0)) break;
            }
            if ((queue.Count / (float)batchSize) < percent) pullingBlocking.Add(1);//激发拉数据

            if (list.Count < 1)
            {
                //当没数据时BLOCK线程,且在固定时间内未取到数据的话，取消取数据线程
                if (!TakeBlocking(queue, list, Consts.Consumer_AsyncQueueDequeueTimeout))
                {
                    long vid;
                    _consumerTask.TryRemove(consumer, out vid);
                    CancellationTokenSource cancellation;
                    if (_cancellationTokenDict.TryGetValue(vid, out cancellation))
                        cancellation.Cancel();
                }
            }
            return list;
        }

        private void Task(string consumer, int batchSize, Func<string, int, CancellationToken, SubChunk> func)
        {
#if DEBUG
            debugLog.Write(string.Format("Start Task."), consumer);
#endif
            long id;
            if (!_consumerTask.TryGetValue(consumer, out id))
            {
                Logg.Write("consumer没有对应的TASK.", LogLevel.Info, new KeyValue { Key = "consumer", Value = consumer });
                return;
            }
            if (!_cancellationTokenDict.ContainsKey(id))
            {
                Logg.Write("consumer没有对应的CancellTokenSource.", LogLevel.Info, new KeyValue { Key = "consumer", Value = consumer });
                return;
            }
            //开线程拉数据
            try
            {
                new Task(() =>
                {
                    try
                    {

                        long taskId;
                        if (!_consumerTask.TryGetValue(consumer, out taskId)) return;//获取当前线程ID
#if DEBUG
                        debugLog.Write(string.Format("Taskid->{0}.", taskId), consumer);
#endif
                        //获取消息队列及是否需要取数据队列
                        BlockingCollection<CBox<ChunkSubMessage>> blocking;
                        BlockingCollection<int> pullingBlocking;
                        if (!_asyncQueue.TryGetValue(consumer, out blocking)
                            || !_pullingBlocking.TryGetValue(consumer, out pullingBlocking))
                        {
                            long vid;
                            _consumerTask.TryRemove(consumer, out vid);//移除当前线程ID
                            CancellationTokenSource cancellation;
                            _cancellationTokenDict.TryRemove(taskId, out cancellation);//移除当前线程的CANCELL
                            return;
                        }

                        if (_cancellationTokenDict[taskId].Token.IsCancellationRequested)
                        {
                            CancellationTokenSource cancellation;
                            _cancellationTokenDict.TryRemove(taskId, out cancellation);//如当前线程CANCELL了，移除队列中CANCELL值
                            return;
                        }

                        while (true)
                        {
                            try
                            {
                                long currentId;
                                if (!_consumerTask.TryGetValue(consumer, out currentId)) return;//获取当前线程ID
                                if (currentId != taskId)
                                {
                                    CancellationTokenSource cancellation;
                                    _cancellationTokenDict.TryRemove(taskId, out cancellation);//如当前线程CANCELL了，移除队列中CANCELL值
                                    return;
                                }

                                int i;
                                pullingBlocking.TryTake(out i, 2000);//如不需要拉数据，BLOCK 2秒，如有及时触发
                                if (_cancellationTokenDict[currentId].Token.IsCancellationRequested)
                                {
                                    CancellationTokenSource cancellation;
                                    _cancellationTokenDict.TryRemove(currentId, out cancellation);//如当前线程CANCELL了，移除队列中CANCELL值
                                    return;
                                }
                                if ((blocking.Count / (float)batchSize) > Consts.Consumer_Percent) continue;

                                var chunk = func(consumer, batchSize, _cancellationTokenDict[currentId].Token);
                                if (chunk == null || chunk.Messages == null) continue;
                                //添加数据
                                var total = chunk.Messages.Count;
                                for (var j = 0; j < total; j++)
                                {
                                    var subMessage = chunk.Messages[j];
                                    if (subMessage.Pub == null)
                                    {
                                        MetricUtil.Set(new PullingDiscardCountMetric { Consumer = consumer });
                                        //记录丢弃的消息数
                                        continue;
                                    }
                                    if (!MomoryManager.AtomicAdd(consumer, subMessage.Pub.Size))
                                    {
                                        MetricUtil.Set(new PullingDiscardCountMetric { Consumer = consumer }, (total - j));
                                        //记录丢弃的消息数
                                        break;
                                    }
                                    blocking.Add(new CBox<ChunkSubMessage>
                                    {
                                        Value = new ChunkSubMessage
                                        {
                                            Position = subMessage.Position,
                                            Pub = subMessage.Pub,
                                            ServerHostName = chunk.ServerName,
                                            Timestamp = chunk.Timestamp
                                        }
                                    });
#if DEBUG
                                    debugLog.Write(string.Format("AddToBlocking->{0} , {1}", subMessage.Position.BatchID, subMessage.Position.Index), consumer);
#endif
                                }
                            }
                            catch (Exception exception)
                            {
                                Logg.Write(exception, LogLevel.Error,
                                             "consumer.consumerqueue.task",
                                             new[] { new KeyValue { Key = "consumer", Value = consumer } });
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Logg.Write(ex, LogLevel.Error, "cmessaging.consumer.queuemanager.task");
                    }
                }, _cancellationTokenDict[id].Token).Start();
            }
            catch (Exception ex)
            {
                if (_cancellationTokenDict.ContainsKey(id))
                    _cancellationTokenDict[id].Cancel();
                Logg.Write(ex, LogLevel.Error, "consumer.consumerqueue.task",
                             new[] { new KeyValue { Key = "consumer", Value = consumer } });
            }
        }

        private bool TakeBlocking(BlockingCollection<CBox<ChunkSubMessage>> queue, List<ChunkSubMessage> list, int timeout)
        {
            CBox<ChunkSubMessage> message;
            if (timeout > 0)
            { if (!queue.TryTake(out message, timeout)) return false; }
            else
            { if (!queue.TryTake(out message)) return false; }

            if (message == null) return false;
            var tmp = message.Value;
            list.Add(tmp);
            message.Value = null;//用于从内存中清除数据，因ConcurrentQueue在4.0中有BUG
            return true;
        }

        public void Remove(string uri)
        {
            ConcurrentQueue<CBox<ChunkSubMessage>> queue;
            if (_syncQueue.TryRemove(uri, out queue))
            {
                while (queue.Count > 0)
                {
                    CBox<ChunkSubMessage> message;
                    if (queue.TryDequeue(out message))
                    {
                        MomoryManager.AtomicReduce(uri, message.Value.Pub.Size);
                        MetricUtil.Set(new PullingDiscardCountMetric { Consumer = uri });
                        message.Value = null;
                    }
                }
            }
            BlockingCollection<CBox<ChunkSubMessage>> block;
            if (_asyncQueue.TryRemove(uri, out block))
            {
                while (block.Count > 0)
                {
                    CBox<ChunkSubMessage> message;
                    if (block.TryTake(out message, 10))
                    {
                        MomoryManager.AtomicReduce(uri, message.Value.Pub.Size);
                        MetricUtil.Set(new PullingDiscardCountMetric { Consumer = uri });
                        message.Value = null;
                    }
                }
            }
            long id;
            if (_consumerTask.TryRemove(uri, out id))
            {
                _consumerTask.TryRemove(uri, out id);
                _cancellationTokenDict[id].Cancel();
            }
        }

        public void Dispose()
        {
            while (_syncQueue.Keys.Count > 0)
            {
                var uris = new string[_syncQueue.Keys.Count];
                _syncQueue.Keys.CopyTo(uris, 0);
                foreach (var uri in uris)
                {
                    Remove(uri);
                }
            }
            while (_asyncQueue.Keys.Count > 0)
            {
                var uris = new string[_asyncQueue.Keys.Count];
                _asyncQueue.Keys.CopyTo(uris, 0);
                foreach (var uri in uris)
                {
                    Remove(uri);
                }
            }
            while (_consumerTask.Keys.Count > 0)
            {
                var uris = new string[_consumerTask.Keys.Count];
                _consumerTask.Keys.CopyTo(uris, 0);
                foreach (var uri in uris)
                {
                    Remove(uri);
                }
            }
        }
    }
}
