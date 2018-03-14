using System;
using System.Linq;
using System.Threading.Tasks;
using DSLink.Connection;
using DSLink.Logger;
using DSLink.Nodes;
using DSLink.Respond;
using Newtonsoft.Json.Linq;
using DSLink.Util;

namespace DSLink.Request
{
    /// <summary>
    /// The requester module of a DSLink gives the ability access to
    /// outer data on the broker.
    /// </summary>
    public class DSLinkRequester
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        private readonly DSLinkContainer _link;
        internal readonly IncrementingIndex RequestId;

        public Connector Connector
        {
            get;
            set;
        }

        public RequestManager RequestManager
        {
            get;
            set;
        }

        public RemoteSubscriptionManager RemoteSubscriptionManager
        {
            get;
            set;
        }

        public DSLinkRequester(DSLinkContainer link)
        {
            _link = link;
            RequestId = new IncrementingIndex(1);
        }

        /// <summary>
        /// Send request to list a path.
        /// </summary>
        /// <param name="path">Remote path to list</param>
        /// <param name="callback">Callback event</param>
        public async Task<ListRequest> List(string path, Action<ListResponse> callback)
        {
            var request = new ListRequest(RequestId.Next, callback, path);
            RequestManager.StartRequest(request);
            await _link.Connector.Send(new JObject
            {
                new JProperty("requests", new JArray
                {
                    request.Serialize()
                })
            });
            return request;
        }

        /// <summary>
        /// Set the specified path's value.
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="permission">Permission</param>
        /// <param name="value">Value</param>
        public async Task<SetRequest> Set(string path, Permission permission, Value value)
        {
            var request = new SetRequest(RequestId.Next, path, permission, value);
            RequestManager.StartRequest(request);
            await _link.Connector.Send(new JObject
            {
                new JProperty("requests", new JArray
                {
                    request.Serialize()
                })
            });
            return request;
        }

        /// <summary>
        /// Remove the specified path.
        /// </summary>
        /// <param name="path">Path</param>
        public async Task<RemoveRequest> Remove(string path)
        {
            var request = new RemoveRequest(RequestId.Next, path);
            RequestManager.StartRequest(request);
            await _link.Connector.Send(new JObject
            {
                new JProperty("requests", new JArray
                {
                    request.Serialize()
                })
            });
            return request;
        }

        /// <summary>
        /// Invoke the specified path.
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="permission">Permission</param>
        /// <param name="parameters">Parameters</param>
        /// <param name="callback">Callback</param>
        public async Task<InvokeRequest> Invoke(string path, Permission permission, JObject parameters, Action<InvokeResponse> callback)
        {
            var request = new InvokeRequest(RequestId.Next, path, permission, parameters, callback);
            RequestManager.StartRequest(request);
            await _link.Connector.Send(new JObject
            {
                new JProperty("requests", new JArray
                {
                    request.Serialize()
                })
            });
            return request;
        }

        /// <summary>
        /// Subscribe to the specified path.
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="callback">Callback</param>
        /// <param name="qos">Quality of Service</param>
        public async Task<int> Subscribe(string path, Action<SubscriptionUpdate> callback, int qos = 0)
        {
            // TODO: Test for quality of service changes.

            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Path can not be null or empty.");
            }

            return await RemoteSubscriptionManager.Subscribe(RequestId.Next, path, callback, qos);
        }

        /// <summary>
        /// Unsubscribe from a subscription ID.
        /// </summary>
        /// <param name="subId">Subscription ID to unsubscribe from.</param>
        public async Task Unsubscribe(int subId)
        {
            await RemoteSubscriptionManager.Unsubscribe(RequestId.Next, subId);
        }

        internal async Task<JArray> ProcessResponses(JArray responses)
        {
            var requests = new JArray();

            foreach (var jToken in responses)
            {
                var response = (JObject) jToken;
                await ProcessResponse(response);
            }

            return requests;
        }

        private async Task ProcessResponse(JObject response)
        {
            if (response["rid"] == null || response["rid"].Type != JTokenType.Integer)
            {
                Log.Warning("Incoming request has invalid or null request ID.");
                return;
            }

            var rid = response["rid"].Value<int>();

            if (rid == 0)
            {
                ProcessValueUpdates(response);
            }
            else if (RequestManager.RequestPending(rid))
            {
                await ProcessRequestUpdates(response, rid);
            }
        }

        private void ProcessValueUpdates(JObject response)
        {
            foreach (dynamic update in response["updates"])
            {
                switch (update)
                {
                    case JArray _:
                        ProcessUpdateArray(update);
                        break;
                    case JObject _:
                        ProcessUpdateObject(update);
                        break;
                }
            }
        }

        private void ProcessUpdateArray(JArray update)
        {
            var sid = update[0].Value<int>();
            var value = update[1];
            var dt = update[2].Value<string>();
            RemoteSubscriptionManager.InvokeSubscriptionUpdate(sid, new SubscriptionUpdate(sid, value, dt));
        }

        private void ProcessUpdateObject(JObject update)
        {
            var sid = update["sid"].Value<int>();
            var value = update["value"];
            var ts = update["ts"].Value<string>();
            var count = update["count"].Value<int>();
            var sum = update["sum"].Value<int>();
            var min = update["min"].Value<int>();
            var max = update["max"].Value<int>();
            RemoteSubscriptionManager.InvokeSubscriptionUpdate(sid, new SubscriptionUpdate(sid, value, ts, count, sum, min, max));
        }

        private async Task ProcessRequestUpdates(JObject response, int rid)
        {
            var request = RequestManager.GetRequest(rid);
            
            switch (request)
            {
                case ListRequest _:
                    var listRequest = (ListRequest) request;
                    var name = listRequest.Path.Split('/').Last();
                    var node = new RemoteNode(name, null, listRequest.Path);
                    node.FromSerialized(response["updates"].Value<JArray>());
                    await Task.Run(() => listRequest.Callback(
                        new ListResponse(_link.Connector, RequestManager, rid, listRequest.Path, node)));
                    break;
                case SetRequest _:
                    RequestManager.StopRequest(rid);
                    break;
                case RemoveRequest _:
                    RequestManager.StopRequest(rid);
                    break;
                case InvokeRequest _:
                    var invokeRequest = (InvokeRequest) request;
                    var path = invokeRequest.Path;
                    var columns = response.GetValue("columns") != null
                        ? response["columns"].Value<JArray>() : new JArray();
                    var updates = response.GetValue("updates") != null
                        ? response["updates"].Value<JArray>() : new JArray();
                
                    await Task.Run(() =>
                    {
                        invokeRequest.Callback(
                            new InvokeResponse(_link.Connector, RequestManager, rid, path, columns, updates));
                    });
                    break;
            }
        }
    }
}
