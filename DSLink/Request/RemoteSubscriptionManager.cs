using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSLink.Connection;
using DSLink.Logger;
using Newtonsoft.Json.Linq;
using DSLink.Util;

namespace DSLink.Request
{
    public class RemoteSubscriptionManager
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        private readonly Connector _connector;
        private readonly Dictionary<string, Subscription> _subscriptions;
        private readonly Dictionary<int, string> _subIdToPath;
        private readonly Dictionary<int, string> _realSubIdToPath;
        private readonly IncrementingIndex _subscriptionId;
        
        public RemoteSubscriptionManager(Connector connector)
        {
            _connector = connector;
            _subscriptions = new Dictionary<string, Subscription>();
            _subIdToPath = new Dictionary<int, string>();
            _realSubIdToPath = new Dictionary<int, string>();
            _subscriptionId = new IncrementingIndex();
        }

        public async Task<int> Subscribe(int rid, string path, Action<SubscriptionUpdate> callback, int qos)
        {
            var sid = _subscriptionId.Next;
            var request = new SubscribeRequest(rid, new JArray
            {
                new JObject
                {
                    new JProperty("path", path),
                    new JProperty("sid", sid),
                    new JProperty("qos", qos)
                }
            }, callback);
            if (!_subscriptions.ContainsKey(path))
            {
                _subscriptions.Add(path, new Subscription(sid));
                await _connector.Send(new JObject
                {
                    new JProperty("requests", new JArray
                    {
                        request.Serialize()
                    })
                });
                _realSubIdToPath[sid] = path;
            }
            _subscriptions[path].VirtualSubs[sid] = callback;
            _subIdToPath[sid] = path;

            return sid;
        }

        public async Task Unsubscribe(int rid, int subId)
        {
            var path = _subIdToPath[subId];
            var sub = _subscriptions[path];
            sub.VirtualSubs.Remove(subId);
            _subIdToPath.Remove(subId);
            if (sub.VirtualSubs.Count == 0)
            {
                await _connector.Send(new JObject
                {
                    new JProperty("requests", new JArray
                    {
                        new UnsubscribeRequest(
                            rid,
                            new JArray
                            {
                                sub.RealSubId
                            }
                        ).Serialize()
                    })
                });
                _subscriptions.Remove(path);
                _subIdToPath.Remove(sub.RealSubId);
                _realSubIdToPath.Remove(sub.RealSubId);
            }
        }

        public List<int> GetSubsByPath(string path)
        {
            var sids = new List<int>();

            if (_subscriptions.ContainsKey(path))
            {
                foreach (var sid in _subscriptions[path].VirtualSubs)
                {
                    sids.Add(sid.Key);
                }
            }

            return sids;
        }

        public void InvokeSubscriptionUpdate(int subId, SubscriptionUpdate update)
        {
            if (!_realSubIdToPath.ContainsKey(subId))
            {
                Log.Debug(string.Format("Remote sid {0} was not found in subscription manager", subId));
                return;
            }
            foreach (var i in _subscriptions[_realSubIdToPath[subId]].VirtualSubs)
            {
                i.Value(update);
            }
        }

        private class Subscription
        {
            public Subscription(int subId)
            {
                RealSubId = subId;
            }

            public readonly int RealSubId;
            public readonly Dictionary<int, Action<SubscriptionUpdate>> VirtualSubs = new Dictionary<int, Action<SubscriptionUpdate>>();
        }
    }
}
