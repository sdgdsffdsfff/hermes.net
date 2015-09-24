using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Text;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Util;
using cmessaging.consumer;
using cmessaging.consumer.ack.message;
using cmessaging.consumer.ack.request;
using cmessaging.consumer.ack.response;
using cmessaging.consumer.pulling.message;
using cmessaging.consumer.pulling.request;
using cmessaging.consumer.pulling.response;
using cmessaging.consumer.sync;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Transmit.Thrift.Transport;
#if DEBUG
using Arch.CMessaging.Core.ObjectBuilder;

#endif

namespace Arch.CMessaging.Client.Impl.Consumer
{
    /// <summary>
    /// 默认与服务端通信类
    /// </summary>
    public sealed class DefaultService : IService
    {
        private IClient Client { get; set; }
#if DEBUG
        private IDebugLogWriter debugLog;
#endif
        public DefaultService(IClient client)
        {
            Client = client;

            #if DEBUG
            this.debugLog = ObjectFactory.Current.Get<IDebugLogWriter>(Lifetime.ContainerControlled);
            #endif 
        }
        /// <summary>
        /// 获取订阅者服务列表
        /// </summary>
        /// <param name="exchanges"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public List<ExchangePhysicalServer> GetExchangePhysicalServers(string exchanges, int timeout)
        {
            var cmessagingAdminUrl = ConfigUtil.Instance.CmessagingAdminUrl;
            if (cmessagingAdminUrl != null) cmessagingAdminUrl = cmessagingAdminUrl.Trim();
            if (string.IsNullOrEmpty(cmessagingAdminUrl))
            {
                throw new ConfigurationErrorsException("'cmessaging_consumer_adminurl' setting is not exists or not value.");
            }
            //cmessagingAdminUrl = cmessagingAdminUrl.EndsWith("/")
            //                         ? string.Format("{0}{1}", cmessagingAdminUrl, exchanges)
            //                         : string.Format("{0}/{1}", cmessagingAdminUrl, exchanges);
            //var pageData = wc.DownloadData(cmessagingAdminUrl); get方式
            ConsumerTraceItems.Instance.AdminUrl = cmessagingAdminUrl;
            try
            {
                var wc = new WebClient { Credentials = CredentialCache.DefaultCredentials };
                wc.Headers.Add(HttpRequestHeader.Accept, "json");
                var enc = Encoding.Default;
                var nameValueCollection = new NameValueCollection { { "exchanges", exchanges } };
                var pageData = wc.UploadValues(cmessagingAdminUrl, "POST", nameValueCollection);

                var str = enc.GetString(pageData);
                //var serializer = new Newtonsoft.Json.conve();
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ExchangePhysicalServer>>(str);
            }
            catch (Exception ex)
            {
                Logg.Write(ex, LogLevel.Error, "cmessaging.consumer.defaultservice.getexchangephysicalservers"
                    , new[]{
                            new KeyValue
                                {
                                    Key = "adminsvcurl",
                                    Value =cmessagingAdminUrl
                                }
                            ,
                            new KeyValue
                                {
                                    Key = "exchanges",
                                    Value =exchanges
                                }
                            ,
                        });
                throw ex;
            }
        }

#if DEBUG
        ThreadSafe.Integer countor = new ThreadSafe.Integer(0);
#endif
        /// <summary>
        /// 向服务端拉数据
        /// </summary>
        /// <param name="server"></param>
        /// <param name="receiveTimeout"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public SubChunk Pulling(PhysicalServer server, int receiveTimeout, PullingRequest request)
        {
            Guard.ArgumentNotNull(server, "server");
            Guard.ArgumentNotNull(request, "request");
            Guard.ArgumentNotNullOrEmpty(request.Uri, "request.Uri");
            Guard.ArgumentNotNullOrEmpty(server.ServerDomainName, "server.ServerDomainName");
            Guard.ArgumentNotNullOrEmpty(server.ServerName, "server.ServerName");
            SubChunk chunk= null;
            bool isNotMessage = true;
            long milliseconds=0;
            string errortype = "";
            
            MetricUtil.Set(new PullingRequestCountMetric
            {
                ServerHostName = server.ServerName,
                Consumer = request.Uri
            });
            var statuscode = 0;
            var watch = new Stopwatch();
            DispatcherServiceWrapper.Client client = null;
#if DEBUG
            var index = countor.AtomicIncrementAndGet();
#endif
            try
            {
#if DEBUG
                debugLog.Write(string.Format("Start servicepulling->{0} server->{1}", index, server.ServerName), request.Uri);
#endif
                client = Client.CreateDispatcherServiceClient(server.ServerDomainName, receiveTimeout);
            
                watch.Start();
                chunk = client.Pulling(request);
                isNotMessage = (chunk == null || chunk.Messages == null || chunk.Messages.Count < 1);
                statuscode = chunk == null ? 0 : (int)chunk.StatusCode;
            }
            catch (WebException webException)
            {
                if (client != null) client.Dispose();
                statuscode = (int)Convert(webException.Status);
                errortype = "webexception";
                if (webException.Message.IndexOf("timed out", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    errortype = "timeout";
                }
                throw webException;
            }
            catch (TTransportException te)
            {
                if (client != null) client.Dispose();
                errortype = "ttransportexception";
                
                if ((te.Message.IndexOf("timed out", StringComparison.InvariantCultureIgnoreCase) > 0)||
                    (te.InnerException != null 
                    && (te.InnerException.Message.IndexOf("timed out", StringComparison.InvariantCultureIgnoreCase) > 0)))
                {
                    errortype = "timeout";
                }

                if (te.InnerException != null && te.InnerException is WebException)
                {
                    var webex = (WebException)te.InnerException;
                    statuscode = (int)Convert(webex.Status);
                }
                else
                {
                    statuscode = (int)Convert(te.Type);
                }
                throw te;
            }
            catch (Exception ex)
            {
                if (client != null) client.Dispose();
                errortype = "systemexception";
                throw ex;
            }
            finally
            {
                watch.Stop();
                milliseconds = watch.ElapsedMilliseconds;
                if(!isNotMessage && chunk !=null && chunk.Messages !=null)
                {
                    var messageSize = 0;
                    var messageCount = chunk.Messages.Count;
                    chunk.Messages.Iterate(x => messageSize += x.Pub != null ? x.Pub.Size : 0);
                    MetricUtil.Set(new PullingMessageCountMetric { MessageSize = messageSize, Consumer = request.Uri }, messageCount);
                }
#if DEBUG
                var messageCount1 = (chunk == null || chunk.Messages == null) ? 0 : chunk.Messages.Count;
                debugLog.Write(string.Format("End servicepulling->{0} messagecount->{1} time->{2}", index, isNotMessage ? "0" : messageCount1.ToString(), milliseconds), request.Uri);
#endif
                MetricUtil.Set(new PullingResponseCountMetric
                {
                    Consumer = request.Uri,
                    ServerHostName = server.ServerName,
                    Latency = milliseconds,
                    StatusCode = statuscode.ToString(),
                    HasMessages = isNotMessage?"0":"1",
                    Error = errortype
                });
                MetricUtil.Set(new PullingResponseLatencyMetric { ServerHostName = server.ServerName, Consumer = request.Uri }, milliseconds);
            }
            return chunk;
        }
        /// <summary>
        /// 向服务端ACK
        /// </summary>
        /// <param name="server"></param>
        /// <param name="timeout"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public ChunkAck Ack(PhysicalServer server, int timeout, ConsumerAckChunk chunk)
        {
            Guard.ArgumentNotNull(server, "server");
            Guard.ArgumentNotNull(chunk, "chunk");
            Guard.ArgumentNotNullOrEmpty(server.ServerDomainName, "server.ServerDomainName");
            Guard.ArgumentNotNullOrEmpty(server.ServerName, "server.ServerName");

            MetricUtil.Set(new AckRequestCountMetric { ServerHostName = server.ServerName });
            foreach (var ack in chunk.ConsumerAcks)
            {
                MetricUtil.Set(new AckMessageCountMetric { Consumer = ack.Uri });
            }

            ChunkAck chunkAck=null;
            var watch = new Stopwatch();
            DispatcherServiceWrapper.Client client=null;
            try
            {
                client = Client.CreateDispatcherServiceClient(server.ServerDomainName, timeout);
                watch.Start();
                chunkAck = client.Ack(chunk);

                return chunkAck;
            }
            catch {
                if (client != null) client.Dispose();
                throw;
            }
            finally
            {
                watch.Stop();
                var milliseconds = watch.ElapsedMilliseconds;
                MetricUtil.Set(new AckResponseCountMetric { ServerHostName = server.ServerName, Latency = milliseconds, StatusCode = chunkAck == null ? "0" : chunkAck.StatusCode.ToString() });
                MetricUtil.Set(new AckResponseLatencyMetric { ServerHostName = server.ServerName }, milliseconds);
            }
        }

        private StatusCode Convert(WebExceptionStatus status)
        {
            switch (status)
            {
                case WebExceptionStatus.NameResolutionFailure:
                    return StatusCode.NameResolutionFailure;
                case WebExceptionStatus.ConnectFailure:
                    return StatusCode.ConnectFailure;
                case WebExceptionStatus.ReceiveFailure:
                    return StatusCode.ReceiveFailure;
                case WebExceptionStatus.SendFailure:
                    return StatusCode.SendFailure;
                case WebExceptionStatus.PipelineFailure:
                    return StatusCode.PipelineFailure;
                case WebExceptionStatus.RequestCanceled:
                    return StatusCode.RequestCanceled;
                case WebExceptionStatus.ProtocolError:
                    return StatusCode.ProtocolError;
                case WebExceptionStatus.ConnectionClosed:
                    return StatusCode.ConnectionClosed;
                case WebExceptionStatus.SecureChannelFailure:
                    return StatusCode.SecureChannelFailure;
                case WebExceptionStatus.TrustFailure:
                    return StatusCode.TrustFailure;
                case WebExceptionStatus.ServerProtocolViolation:
                    return StatusCode.ServerProtocolViolation;
                case WebExceptionStatus.KeepAliveFailure:
                    return StatusCode.KeepAliveFailure;
                case WebExceptionStatus.Pending:
                    return StatusCode.Pending;
                case WebExceptionStatus.Timeout:
                    return StatusCode.Timeout;
                case WebExceptionStatus.ProxyNameResolutionFailure:
                    return StatusCode.ProxyNameResolutionFailure;
                case WebExceptionStatus.UnknownError:
                    return StatusCode.UnknownError;
                case WebExceptionStatus.MessageLengthLimitExceeded:
                    return StatusCode.MessageLengthLimitExceeded;
                case WebExceptionStatus.CacheEntryNotFound:
                    return StatusCode.CacheEntryNotFound;
                case WebExceptionStatus.RequestProhibitedByCachePolicy:
                    return StatusCode.RequestProhibitedByCachePolicy;
                case WebExceptionStatus.RequestProhibitedByProxy:
                    return StatusCode.RequestProhibitedByProxy;
                default:
                    return StatusCode.UnknownError;
            }
        }

        private StatusCode Convert(TTransportException.ExceptionType type)
        {
            switch (type)
            {
                case TTransportException.ExceptionType.NotOpen:
                    return StatusCode.NotOpen;
                case TTransportException.ExceptionType.Unknown:
                    return StatusCode.Unknown;
                case TTransportException.ExceptionType.AlreadyOpen:
                    return StatusCode.AlreadyOpen;
                case TTransportException.ExceptionType.EndOfFile:
                    return StatusCode.EndOfFile;
                case TTransportException.ExceptionType.TimedOut:
                    return StatusCode.Timeout;
                default:
                    return StatusCode.Unknown;
            }
        }
    }
}
