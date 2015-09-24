using System;
using System.Collections.Generic;
using System.Threading;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using cmessaging.consumer.exception;
using cmessaging.consumer.noserver;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal sealed class InputBuffer : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly ServerPool _serverPool;
        private readonly  QueueManager _consumerQueue;
        private readonly ServerUriManager _serverUriManager;

        public InputBuffer(IService server)
        {
            Guard.ArgumentNotNull(server, "server");

            _serverPool = new ServerPool(server);
            _cts = new CancellationTokenSource();
            _serverUriManager = new ServerUriManager();
            _consumerQueue = new QueueManager();
        }

        public MemoryManager MomoryManager { get { return _consumerQueue.MomoryManager; } set { _consumerQueue.MomoryManager = value; } }

        public int AckTimeout { get; set; }

        private uint _capacity;
        public uint Capacity
        {
            get { return _capacity; }
            set
            {
                _capacity = value;
                MomoryManager.ChangeMaxMemorySize(value);
            }
        }

        public ushort ConnectionMax
        {
            set { _serverPool.MaxPoolSize = value; }
        }

        /// <summary>
        /// 清空uri对应的数据
        /// </summary>
        /// <param name="uri"></param>
        public void Remove(string uri)
        {
            _consumerQueue.Remove(uri);
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        /// <param name="exchangeServers"></param>
        public void RegisterServer(Dictionary<string,List<ExchangePhysicalServer>> exchangeServers)
        {
            _serverUriManager.RegisterServer(exchangeServers);
        }
        /// <summary>
        /// 获取消息
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="batchSize"></param>
        /// <param name="receiveTimeout"></param>
        /// <param name="sync"></param>
        /// <returns></returns>
        public List<ChunkSubMessage> GetChunkSubMessages(string uri, int batchSize, int receiveTimeout, bool sync)
        {
            Guard.ArgumentNotNullOrEmpty(uri, "uri");
            if (batchSize < 1) throw new ArgumentException("batchSize is a positive non-zero integer!");

            return sync
                        ? _consumerQueue.Take(uri, batchSize, (u, size) => PullingSync(u, size, receiveTimeout))
                        : _consumerQueue.TakeAsync(uri, batchSize,
                                                    (u, size, cancellToken) => PullingAsync(u, size, receiveTimeout, cancellToken));
        }
        /// <summary>
        /// 同步请求处理
        /// </summary>
        /// <param name="u"></param>
        /// <param name="size"></param>
        /// <param name="receiveTimeout"></param>
        /// <returns></returns>
        private SubChunk PullingSync(string u, int size,int receiveTimeout)
        {
            //获取服务器
            var server = _serverUriManager.Schedule(u,receiveTimeout, true);
            if (server == null)
            {
                MetricUtil.Set(new NoServerCountMetric { Consumer = u });//记录没有取得服务器信息
                System.Threading.Thread.Sleep(3000);
                return null;
            }

            bool isNotMessage = true;
            var pullingTime = DateTime.Now;
            SubChunk chunk;
            try
            {
                chunk = Pulling(u, size, receiveTimeout, server, false);//从服务器取数据
                isNotMessage = (chunk == null || chunk.Messages == null || chunk.Messages.Count < 1);
            }
            finally
            {
                if (isNotMessage)
                {
                    var milliseconds = (int)(DateTime.Now - pullingTime).TotalMilliseconds;
                    if (receiveTimeout > milliseconds)
                    {
                        System.Threading.Thread.Sleep(receiveTimeout - milliseconds); //如未达到时间，BLOCK一段时间
                    }
                }
            }
            if (chunk != null
                    && chunk.StatusCode != StatusCode.OK
                    && chunk.StatusCode != StatusCode.Accepted
                    && chunk.StatusCode != StatusCode.ThrottlingRequired)
            {
                //记录日志
                Logg.Write("Load data fail,reason:" + chunk.StatusCode.ToString(),
                    LogLevel.Warn,
                    Consts.Consumer_Title_LoadDataError,
                    new[]
                                                                    {
                                                                        new KeyValue {Key = "Consumer", Value = u},
                                                                        new KeyValue {Key = "ServerDomainName",Value = server.ServerDomainName},
                                                                        new KeyValue {Key = "ServerName", Value = server.ServerName }
                                                                    });
            }
            return chunk;
        }
        /// <summary>
        /// 异步请求处理
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="batchSize"></param>
        /// <param name="receiveTimeout"></param>
        /// <param name="cancell"></param>
        /// <returns></returns>
        private SubChunk PullingAsync(string uri, int batchSize, int receiveTimeout,CancellationToken cancell)
        {
            if (_cts != null && _cts.IsCancellationRequested) return null;//INPUTBUFFER 线程结束

            PhysicalServer server = null;
            try
            {
                if (cancell.IsCancellationRequested) return null;

                server = _serverUriManager.Schedule(uri,receiveTimeout,false);
                if (server == null)
                {
                    MetricUtil.Set(new NoServerCountMetric {Consumer = uri});
                    //记录没有取得服务器信息
                    System.Threading.Thread.Sleep(3000);
                    return null;//PullingAsync(uri, batchSize, receiveTimeout, cancell);
                }
                if (_cts != null && _cts.IsCancellationRequested) return null;//INPUTBUFFER 线程结束
                if (cancell.IsCancellationRequested) return null;//线程取消
                var chunk = Pulling(uri, batchSize, receiveTimeout, server); //从服务器取数据
                var hasMessages = (chunk != null && chunk.Messages != null && chunk.Messages.Count > 0);
                if (chunk != null
                    && chunk.StatusCode != StatusCode.OK
                    && chunk.StatusCode != StatusCode.Accepted
                    && chunk.StatusCode != StatusCode.ThrottlingRequired)
                {
                    //记录日志
                    Logg.Write("Load data fail,reason:" + chunk.StatusCode.ToString(),
                                 LogLevel.Warn,
                                 Consts.Consumer_Title_LoadDataError,
                                 new[]
                                     {
                                         new KeyValue {Key = "Consumer", Value = uri},
                                         new KeyValue {Key = "ServerDomainName", Value = server.ServerDomainName},
                                         new KeyValue {Key = "ServerName", Value = server.ServerName}
                                     });
                }
                return hasMessages ? chunk : PullingAsync(uri, batchSize, receiveTimeout, cancell);
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "consumer.inputbuffer.pullingasync", new[]
                                                   {
                                                       new KeyValue{Key="Consumer",Value=uri},
                                                       new KeyValue{Key="ServerUri",Value = server==null?"":server.ServerDomainName},
                                                       new KeyValue{Key="BatchSize",Value = batchSize.ToString()},
                                                       new KeyValue{Key="ReceiveTimeOut",Value = receiveTimeout.ToString()}, 
                                                   });
               // return PullingAsync(uri, batchSize, receiveTimeout,cancell);
                return null;
            }
        }

        /// <summary>
        /// 往SERVER请求加载数据
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="batchSize"></param>
        /// <param name="receiveTimeout"></param>
        /// <param name="server"> </param>
        /// <param name="sync"> </param>
        private SubChunk Pulling(string uri, int batchSize, int receiveTimeout, PhysicalServer server,bool sync= true)
        {
            SubChunk chunk = null;
            try
            {
                _serverPool.AcquireTimeout = receiveTimeout;
                var service = _serverPool.Acquire(); //SERVER线程数控制
                if (MomoryManager.IsOutOfMaxMemorySize)
                {
                    Logg.Write("当前内存超过最大使用内存", LogLevel.Warn, "Consumer.InputBuffer.LoadData",
                                 new[]
                                     {
                                         new KeyValue {Key = "Consumer", Value = uri},
                                         new KeyValue
                                             {Key = "MaxMemorySize", Value = MomoryManager.MaxMemorySize.ToString()},
                                         new KeyValue
                                             {
                                                 Key = "CurrentMemorySize",
                                                 Value = MomoryManager.CurrentMemorySize.ToString()
                                             }
                                     });
                    _serverPool.Release();
                    return null; //内存控制
                }
                if (_cts != null && _cts.IsCancellationRequested)
                {
                    _serverPool.Release();
                    return null;
                }
                try
                {
                    chunk = service.Pulling(server,
                                            receiveTimeout,
                                            new PullingRequest
                                                {
                                                    AckTimeout = AckTimeout,
                                                    Timestamp = Time.ToTimestamp(),
                                                    Uri = uri,
                                                    BatchSize = batchSize,
                                                    ClientIP = Local.IPV4,
                                                    ClientVersion = Version.AssemblyMajorVersion,
                                                    Platform = "NET"
                                                });
                    if (chunk == null) throw new Exception("pulling return null");
                }
                catch
                {
                    MetricUtil.Set(new ExceptionCountMetric { Consumer = uri, HappenedWhere = ExceptionType.OnPulling.ToString() });
                    throw;
                }
                finally
                {
                    _serverPool.Release();
                    _serverUriManager.EndPulling(uri, server,(chunk != null && chunk.Messages != null && chunk.Messages.Count > 0));
                }
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error,
                             Consts.Consumer_Title_LoadDataError,
                             new[]
                                 {
                                     new KeyValue {Key = "PullingRequestUri", Value = uri},
                                     new KeyValue {Key = "ServerUri", Value = server == null ? "" : server.ServerDomainName},
                                 });
            }
            return chunk;
        }
        
        /// <summary>
        /// 从uri获取exchange
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string GetExchange(string uri)
        {
            var arr = uri.Split('/');
            var exchange = arr.Length > 2 ? arr[2].Trim() : "";
            return exchange;
        }

        public void RegisterConsumer(string exchange,string uri)
        {
            if (!string.IsNullOrEmpty(exchange) && !string.IsNullOrEmpty(uri))
            {
                _serverUriManager.RegisterConsumer(exchange, uri);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _consumerQueue.Dispose();
        }

    }
}
