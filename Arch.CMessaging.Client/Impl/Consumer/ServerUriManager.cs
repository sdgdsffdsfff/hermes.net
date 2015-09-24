using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Arch.CMessaging.Client.Impl.Consumer.AppInternals;
using Arch.CMessaging.Client.Impl.Consumer.Models;
using Arch.CMessaging.Core.Log;
using Arch.CMessaging.Core.ObjectBuilder;
using Arch.CMessaging.Core.Scheduler;
using Arch.CMessaging.Core.Content;

namespace Arch.CMessaging.Client.Impl.Consumer
{
    internal class ServerUriManager
    {
        //consumer,ServerName,出错次数，退避次数
        private readonly ConcurrentDictionary<string, Dictionary<string, Tuple<int, int, DateTime>>> _noMessageList = new ConcurrentDictionary<string, Dictionary<string, Tuple<int, int, DateTime>>>();
        private readonly object _lockObject = new object();
        //consumer,wrs
        private readonly ConcurrentDictionary<string, WeightedRandomScheduling<PhysicalServer>> _consumerWrsList;
        private readonly ConcurrentDictionary<string, List<string>> _exchangeConsumers;
        private Random random;
        public ServerUriManager()
        {
            _consumerWrsList = new ConcurrentDictionary<string, WeightedRandomScheduling<PhysicalServer>>();
            _exchangeConsumers = new ConcurrentDictionary<string, List<string>>();

            long tick = DateTime.Now.Ticks;
            random = new Random((int)(tick & 0xffffffffL));
        }

        public void RegisterServer(Dictionary<string, List<ExchangePhysicalServer>> exchangeServers)
        {
            if (exchangeServers == null) exchangeServers = new Dictionary<string, List<ExchangePhysicalServer>>();

            //处理现有exchange对应的SERVER
            foreach (var exchange in exchangeServers.Keys)
            {
                var exchangeLower = exchange.ToLower().Trim();

                List<string> consumers;
                if (!_exchangeConsumers.TryGetValue(exchangeLower, out consumers)) continue;
                //循环所有consumer
                var total = consumers.Count;
                for (var i = 0; i < total; i++)
                {
                    if (consumers.Count < i) break;
                    var consumer = consumers[i];
                    var wrs = _consumerWrsList.GetOrAdd(consumer, s => new WeightedRandomScheduling<PhysicalServer>());//获取或添加Consumer
                    var serverList = exchangeServers[exchange];//服务列表

                    //循环WRS原有SERVER列表
                    foreach (var server in wrs.Servers)
                    {
                        var ss = serverList.FirstOrDefault(s => s.Equals(server)); //根据SERVERDOMAINNAME获取SERVER
                        if (ss == null) server.Mark = false;//打标记
                        else
                        {
                            server.Mark = true;
                            wrs.ChangeWeight(server, ss.Weight);//修改权重
                        }
                    }
                    //循环新SERVER列表
                    foreach (var item in serverList)
                    {
                        if (string.IsNullOrEmpty(item.ServerDomainName) ||
                            string.IsNullOrEmpty(item.ServerIP) ||
                            string.IsNullOrEmpty(item.ServerName)) continue;
                        if (wrs.Servers.Any(s => s.Equals(item))) continue;
                        wrs.Register(item, item.Weight);
                    }
                }
            }
        }

        public void RegisterConsumer(string exchange, string uri)
        {
            if (exchange == null) return;
            _exchangeConsumers.AddOrUpdate(exchange.ToLower().Trim(), s => new List<string> { uri }, (s, list) =>
            {
                if (!list.Contains(uri))
                    list.Add(uri);
                return list;
            });
        }

        public PhysicalServer Schedule(string consumer, int receiveTimeout,bool sync)
        {
            const string title = "noserver";
            if (string.IsNullOrEmpty(consumer))
            {
                Logger.Write("consumer is null", LogLevel.Warn, title);
                return null;
            }

            WeightedRandomScheduling<PhysicalServer> wrs;
            if (!_consumerWrsList.TryGetValue(consumer, out wrs))
            {
                Logger.Write("consumer is not exists int WRS LIST", LogLevel.Warn, title, new[] { new KeyValue { Key = "consumer", Value = consumer } });
                return null;
            }

            var server = wrs.Schedule();
            if (server == null)
            {
                Logger.Write("no server when server run schedule", LogLevel.Warn, title, new[] { new KeyValue { Key = "consumer", Value = consumer } });
                return null;
            }
            //if (!server.Mark) return server; //Mark标记为false的不作退避
            string serverName = "";
            DateTime date = DateTime.MaxValue;
            lock (_lockObject)
            {
                Dictionary<string, Tuple<int, int, DateTime>> dictionary;
                if (!_noMessageList.TryGetValue(consumer, out dictionary)) return server;//此CONSUMER没有无消息的SERVER,直接返回
                if (!dictionary.ContainsKey(server.ServerName)) return server; //CONSUMER对应此SERVER, 不存在列表中，直接返回
                if ((DateTime.Now - dictionary[server.ServerName].Item3).TotalMilliseconds > receiveTimeout / 2) return server;//如超500ms，则继续请求

                var tmp = dictionary.ToList();
                if (tmp.Count >= wrs.Servers.Length)
                {
                    PhysicalServer[] servers = new PhysicalServer[wrs.Servers.Length];
                    wrs.Servers.CopyTo(servers, 0);
                    //当都没有数据时，取最长时间未取数据的SERVER
                    foreach (var item in tmp)
                    {
                        var s = servers.FirstOrDefault(x => x.ServerName == item.Key);//如在server中不存在的，不作处理
                        if (s == null) continue;
                        if ( date > item.Value.Item3)
                        {
                            date = item.Value.Item3;
                            serverName = item.Key;
                            server = s;
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(serverName))
            {
                if (!sync) Block(consumer, date, receiveTimeout);//异步做休眠
                return server;
            }
            return Schedule(consumer,receiveTimeout,sync);
        }
        //PULLING请求结束时调用
        public void EndPulling(string consumerUri, PhysicalServer server, bool hasMessages)
        {
            if (server == null) return;
            if (hasMessages)
            {
                RemoveServer(consumerUri, server);//有数据从无数据列表中迁除该SERVER
            }
            else
            {
                AddOrUpdateServer(consumerUri, server.ServerName);
                if (server.Mark == false)
                    RemoveMarkServer(consumerUri, server.ServerName);
            }
        }
        //移除被标记服务
        private void RemoveMarkServer(string consumer, string serverName)
        {
            if (string.IsNullOrEmpty(consumer)) return;
            Dictionary<string, Tuple<int, int, DateTime>> dict;
            if (!_noMessageList.TryGetValue(consumer, out dict)) return;
            Tuple<int, int, DateTime> tuple;
            if (!dict.TryGetValue(serverName, out tuple)) return;
            if (tuple.Item1 < ConfigUtil.Instance.MarkServerRemove) return;//判断出错次数是否大于10

            WeightedRandomScheduling<PhysicalServer> wrs;
            if (!_consumerWrsList.TryGetValue(consumer, out wrs)) return;
            if (wrs == null) return;
            var server = wrs.Servers.FirstOrDefault(x => string.Equals(x.ServerName, serverName, StringComparison.InvariantCultureIgnoreCase) && x.Mark == false);
            RemoveServer(consumer, server);
            wrs.Unregister(server);//移除
        }

        private void AddOrUpdateServer(string consumerUri, string serverName)
        {
            //如不存在，添加. 出错次数+1，退避次数归0
            _noMessageList.AddOrUpdate(consumerUri,
                                      d =>
                                      new Dictionary<string, Tuple<int, int, DateTime>> { { serverName, new Tuple<int, int, DateTime>(1, 0, DateTime.Now) } },
                                      (d, v) =>
                                      {
                                          lock (_lockObject)
                                          {
                                              if (v.ContainsKey(serverName))
                                                  v[serverName] = new Tuple<int, int, DateTime>(v[serverName].Item1 + 1, 0, DateTime.Now);
                                              else
                                                  v.Add(serverName, new Tuple<int, int, DateTime>(1, 0, DateTime.Now));
                                              return v;
                                          }
                                      }
                );
        }

        private void RemoveServer(string consumerUri, PhysicalServer server)
        {
            if (server == null) return;
            //移除无数据SERVER
            Dictionary<string, Tuple<int, int, DateTime>> dictionary;
            if (_noMessageList.TryGetValue(consumerUri, out dictionary))
            {
                lock (_lockObject)
                {
                    if (dictionary.ContainsKey(server.ServerName))
                    {
                        dictionary.Remove(server.ServerName);
                    }
                }
            }
        }

        public void Block(string uri, DateTime datetime, int receiveTimeout)
        {
            //死信多休眠一会。
            var milliseconds = (uri.StartsWith("dead:")) ? RandomLongTimes(receiveTimeout) : RandomTimes(receiveTimeout);

            var totalMilliseconds = (int)((DateTime.Now - datetime).TotalMilliseconds);
            if (totalMilliseconds < milliseconds)
            {
                System.Threading.Thread.Sleep(milliseconds - totalMilliseconds);
            }
        }

        /// <summary>
        /// 500~1000毫秒
        /// </summary>
        /// <returns></returns>
        private int RandomTimes(int receiveTimeout)
        {
            return random.Next(receiveTimeout/2, receiveTimeout);
        }
        /// <summary>
        /// 5~10 秒
        /// </summary>
        /// <returns></returns>
        private int RandomLongTimes(int receiveTimeout)
        {
            return random.Next(receiveTimeout * 5, receiveTimeout * 10);
        }
    }
}
