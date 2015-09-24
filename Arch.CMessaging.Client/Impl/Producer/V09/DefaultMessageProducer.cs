using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using Arch.CMessaging.Client.API;
using Arch.CMessaging.Client.Event;
using Arch.CMessaging.Core.CFXMetrics;
using Arch.CMessaging.Core.Content;
using Arch.CMessaging.Core.gen;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.Time;
using Arch.CMessaging.Core.Transmit.Thrift.Protocol;
using Arch.CMessaging.Core.Transmit.Thrift.Transport;
using Arch.CMessaging.Core.Util;
using cmessaging.producer.rcv.nack;
using cmessaging.producer.send;
using cmessaging.producer.send.response;
using cmessaging.producer.exception;

namespace Arch.CMessaging.Client.Impl.Producer.V09
{
    public class DefaultMessageProducer : IMessageProducer
    {
        static int count = 0;
        public event BrokerAckEventHandler BrokerAcks = delegate { };
        public event BrokerNackEventHandler BrokerNacks = delegate { };
        public event CallbackExceptionEventHandler CallbackException = delegate { };
        public event FlowControlEventHandler FlowControl = delegate { };

        internal DefaultMessageChannel Channel { get; set; }

        public uint WindowSize { get; set; }
        public bool UseFlowControl { get; set; }
        public bool IsUnderFlowControl{ get { return UseFlowControl; } }
        public string Identifier { get; set; }
        private string ExchangeName { get; set; }
        private string appId;
        private string AppId
        {
            get
            {
                if (string.IsNullOrEmpty(appId))
                {
                    appId = ConfigurationManager.AppSettings["AppID"];
                }
                return appId;
            }
        }

        public void ExchangeDeclare(string exchangeName)
        {
            Guard.ArgumentNotNullOrEmpty(exchangeName, "exchangeName");
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");

            ExchangeName = exchangeName.Trim();
            Channel.RegisterExchange(ExchangeName, Identifier);//注册exchange
        }

        public void PublishAsync<T>(T message, string subject, MessageHeader header = null)
        {
            PublishAsync(message, CreateDefaultHeaderProperties(message, subject, header));
        }

        public void PublishAsync<T>(T message, IHeaderProperties properties)
        {
            Guard.ArgumentNotNullOrEmpty(ExchangeName, "ExchangeName");
            Guard.ArgumentNotNullOrEmpty(Identifier, "Identifier");
            checkHeader(properties);

            var collects = GetCollects();
            var msg = CreatePubMessage(message, properties);

            if (count == 100000) count = 0;
            count++;
            var index = count % collects.Length;
            var serviceUri = collects[index];
            var result = Send(serviceUri, msg);
            if (result.IsResult) return;
            //重试失败，尝试其它采集服务
            for (var i = index + 1; i < (collects.Length + index - 1); i++)
            {
                var j = i < collects.Length ? i : i - collects.Length;
                serviceUri = collects[j];
                result = Send(serviceUri, msg);
                if (result.IsResult) break;
            }
            if (!result.IsResult) throw result.Exception;
        }

        private PubResult Send(string serviceUri, PubMessage msg)
        {
            MetricManagerFactory.MetricManager.Set(new SendCountMetric
            {
                Identifier = Identifier,
                ExchangeName = msg.ExchangeName,
                ClientVersion = Impl.Version.AssemblyVersion,
                ServerHostname = serviceUri
            });

            string error = "";
            var statuscode=0;
            ExchangeServiceWrapper.Client client = null;
            var result = new PubResult {IsResult = true};
            var watch = new Stopwatch();
            THttpClient transport = null;
            try
            {
                client = CreateClient(serviceUri);
                var chunk = CreatePubChunk(msg);
                watch.Start();
                var ack = client.Publish(chunk);
                statuscode = (int)ack.StatusCode;
                if (ack.Acks != AckMode.Ack)
                {
                    MetricManagerFactory.MetricManager.Set(new RcvNackCountMetric{ExchangeName = msg.ExchangeName,Identifier = Identifier,ServerHostname = serviceUri});
                    result.Exception = new Exception("Send message failed.");
                }
                return result;
            }
            catch (WebException webException)
            {
                statuscode = (int)Convert(webException.Status);
                error = "webexception";
                if (webException.Status == WebExceptionStatus.Timeout)
                {
                    error = "timeout";
                }
                result.Exception = webException;
                MetricManagerFactory.MetricManager.Set(new ProducerExceptionCountMetric { ExchangeName = msg.ExchangeName, Identifier = Identifier, ServerHostname = serviceUri });
            }
            catch (TTransportException te)
            {
                if (te.InnerException != null && te.InnerException is WebException)
                {
                    var webex = (WebException)te.InnerException;
                    statuscode = (int)Convert(webex.Status);
                    error = "ttransportexception.webexception";
                    if (webex.Status == WebExceptionStatus.Timeout)
                    {
                        error = "timeout";
                    }
                }
                else
                {
                    statuscode = (int) Convert(te.Type);
                    error = "ttransportexception";
                    if (te.Type == TTransportException.ExceptionType.TimedOut)
                    {
                        error = "timeout";
                    }
                }
                result.Exception = te;
                MetricManagerFactory.MetricManager.Set(new ProducerExceptionCountMetric { ExchangeName = msg.ExchangeName, Identifier = Identifier, ServerHostname = serviceUri });
            }
            catch (Exception ex)
            {
                error = "systemexception";
                result.Exception = ex;
                MetricManagerFactory.MetricManager.Set(new ProducerExceptionCountMetric { ExchangeName = msg.ExchangeName, Identifier = Identifier, ServerHostname = serviceUri });
            }
            finally
            {
                if (client != null) client.Dispose();
                //if (transport != null)
                //{
                //    transport.Close();
                //    transport.Dispose();
                //}
                if (watch.IsRunning) watch.Stop();
                MetricManagerFactory.MetricManager.Set(new SendResponseCountMetric { ExchangeName = msg.ExchangeName, Identifier = Identifier, ServerHostname = serviceUri, error = error, Latency = watch.ElapsedMilliseconds, StatusCode = statuscode.ToString() });
            }
            if (!result.IsResult && result.Exception.Message != "Send message failed.")
            {
                Logg.Write(result.Exception, LogLevel.Error, "cmessaging.producer.send", new[]
                             {
                                 new KeyValue {Key = "Identifier", Value = Identifier},
                                 new KeyValue {Key = "Exchange", Value = ExchangeName},
                                 new KeyValue {Key = "ServiceUri", Value = serviceUri},
                             });
            }
            return result;
        }

        private PubChunk CreatePubChunk(PubMessage msg)
        {
            var chunk = new PubChunk
            {
                ChunkID = Guid.NewGuid().ToString(),
                ClientIP = Local.IPV4,
                Messages = new List<PubMessage>(),
                Identifier = Identifier,
                ClientVersion = Impl.Version.AssemblyMajorVersion,
                Timestamp = Time.ToTimestamp(),
                ExchangeName = msg.ExchangeName,
                Platform = "NET"
            };

            chunk.Messages.Add(msg);
            return chunk;
        }

        private ExchangeServiceWrapper.Client CreateClient(string serviceUri)
        {
            var timeout = ConfigUtil.Instance.Timeout;
            var uri = new Uri(serviceUri);
            var transport = new THttpClient(uri);
            if (timeout > 0)
            {
                transport.ConnectTimeout = timeout;
                transport.ReadTimeout = timeout;
            }

            TProtocol protocol = new TBinaryProtocol(transport);
            return new ExchangeServiceWrapper.Client(protocol);
        }

        public void Dispose()
        {
        }

        public IHeaderProperties CreateDefaultHeaderProperties(object message, string subject, MessageHeader header)
        {
            var props = new BasicHeader
                            {
                                AppID = AppId,
                                Subject = subject,
                                ClientID = Local.HostName,
                                Compression = CompressionType.GZip,
                                ContentEncoding = "utf-8",
                                ExchangeName = ExchangeName,
                                MessageID = Guid.NewGuid().ToString(),
                                RawType = message.GetType().FullName,
                                CorrelationID = "",
                                Route = Local.IPV4,
                                Sequence = "1",
                                Serialization = SerializationType.Thrift,
                                Timestamp = Time.ToTimestamp(),
                                UserHeader = new Dictionary<string, string>(),
                                Version = "0.9"
                            };
            if (message.GetType().Equals(typeof(byte[])))
            {
                props.Type = MessageType.Binary;
            }
            else if (message.GetType().Equals(typeof(string)))
            {
                props.Type = MessageType.Text;
            }
            else
            {
                props.Type = MessageType.Object;
            }

            if (header != null)
            {
                if (!string.IsNullOrWhiteSpace(header.AppID))
                {
                    props.AppID = header.AppID;
                }

                if (!string.IsNullOrWhiteSpace(header.CorrelationID))
                {
                    props.CorrelationID = header.CorrelationID;
                }

                if (!string.IsNullOrWhiteSpace(header.Sequence))
                {
                    props.Sequence = header.Sequence;
                }

                if (header.UserHeader != null)
                {
                    props.UserHeader = header.UserHeader;
                }
            }
            return props;
        }

        private void checkHeader(IHeaderProperties properties)
        {
            if (properties == null)
            {
                throw new Exception("Header is null.");
            }

            string subject = properties.Subject;
            if (subject == null || subject.Trim().Equals(""))
            {
                throw new Exception("Subject is empty.");
            }
        }

        private string[] GetCollects()
        {
            var collects = Channel.GetCollects(ExchangeName);
            if (collects == null || collects.Length == 0)
            {
                Logg.Write("没有可用的采集服务器!", LogLevel.Warn, "cmessaging.producer.defaultmessageproducer",
                             new[]
                                 {
                                     new KeyValue {Key = "Exchange", Value = ExchangeName},
                                     new KeyValue {Key = "Identifier", Value = Identifier}
                                 });
                MetricManagerFactory.MetricManager.Set(new ProducerExceptionCountMetric { ExchangeName = ExchangeName, Identifier = Identifier });
                throw new Exception("没有可用的采集服务器!");
            }
            return collects;
        }

        private PubMessage CreatePubMessage<T>(T message, IHeaderProperties properties)
        {
            var writer = new MessageWriter();
            writer.Write(message, properties);
            var msg = writer.ToMessage();

            if (msg.Size > Consts.OneMessageMaxSize)
            {
                Logg.Write("Message size is bigger than " + Consts.OneMessageMaxSize + ".", LogLevel.Warn, "cmessaging.producer.defaultmessageproducer",
                                 new[]
                                 {
                                     new KeyValue {Key = "Exchange", Value = ExchangeName},
                                     new KeyValue {Key = "Identifier", Value = Identifier}
                                 });
                throw new Exception("Message size is bigger than " + Consts.OneMessageMaxSize + ".");
            }
            return msg;
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


    internal class PubResult
    {
        public bool IsResult { get; set; }

        private Exception exception;
        public Exception Exception
        {
            get { return exception; }
            set
            {
                exception = value;
                IsResult = false;
            }
        }
    }
}
