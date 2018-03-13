using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSLink.Util;
using DSLink.Broker.Objects;
using DSLink.Connection;
using DSLink.Respond;
using DSLink.Serializer;
using Newtonsoft.Json.Linq;

namespace DSLink.Broker
{
    public class ServerLink
    {
        private static readonly Random Random = new Random();
        private readonly PeriodicTask _pingPeriodicTask;
        private readonly IncrementingIndex _msg = new IncrementingIndex();
        private BaseSerializer _serializer;
        private WebSocket _webSocket;
        
        public readonly string DsId;
        public readonly KeyPair TempKey;
        public bool IsResponder => RequestObject.isResponder;
        public bool IsRequester => RequestObject.isRequester;
        public ConnRequestObject RequestObject
        {
            get;
            set;
        }
        public string NextSalt
        {
            get
            {
                var bytes = new byte[6];
                Random.NextBytes(bytes);
                return BitConverter.ToString(SHA256.ComputeHash(bytes)).Replace("-", "").ToLower();
            }
        }

        public ServerLink(string dsId)
        {
            DsId = dsId;
            _pingPeriodicTask = new PeriodicTask(_pingElapsed, 30000);
            TempKey = new KeyPair();
        }

        private void _recvStringMessage(string str)
        {
            _handleIncomingObject(_serializer.Deserialize(str));
        }

        private void _recvBinaryMessage(byte[] bytes)
        {
            _handleIncomingObject(_serializer.Deserialize(bytes));
        }

        private void _handleIncomingObject(JObject obj)
        {
            Console.WriteLine(obj.ToString());
            var outgoingResponses = new JArray();
            var respObject = new JObject
            {
                new JProperty("responses", outgoingResponses),
                new JProperty("requests", outgoingResponses)
            };
            var sendAck = false;
            
            if (obj.ContainsKey("responses"))
            {
                sendAck = true;
            }

            if (obj.ContainsKey("requests"))
            {
                var requests = obj["requests"];

                foreach (var request in requests)
                {
                    var method = request["method"].Value<string>();
                    var rid = request["rid"].Value<int>();

                    switch (method)
                    {
                        case "list":
                            var path = request["path"];
                            var node = Program.Broker.RootNode.Get(path.Value<string>());
                            if (node != null)
                            {
                                outgoingResponses.Add(new JObject
                                {
                                    new JProperty("rid", rid),
                                    new JProperty("stream", "open"),
                                    new JProperty("updates", SubscriptionManager.SerializeUpdates(node))
                                });
                            }
                            break;
                    }
                }
                
                sendAck = true;
            }

            if (sendAck && obj.ContainsKey("msg"))
            {
                respObject["ack"] = obj["msg"];
            }
            _sendObject(respObject);
        }

        private Task _send(string data)
        {
            return _send(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text);
        }

        private Task _send(byte[] data, WebSocketMessageType msgType = WebSocketMessageType.Binary)
        {
            return _webSocket.SendAsync(data, msgType, true, CancellationToken.None);
        }

        private Task _sendObject(JObject obj)
        {
            if (!obj.ContainsKey("msg"))
            {
                obj["msg"] = _msg.Next;
            }

            return _send(obj.ToString());
        }

        private Task _onConnect()
        {
            return _sendObject(new JObject
            {
                new JProperty("salt", NextSalt),
            });
        }

        private void _pingElapsed()
        {
            _sendObject(new JObject());
        }

        public void InitSerializer(string format)
        {
            switch (format)
            {
                case "json":
                    _serializer = new JsonSerializer();
                    break;
                case "msgpack":
                    _serializer = new MsgPackSerializer();
                    break;
                default:
                    Console.WriteLine("Warning: unknown format: " + format);
                    _serializer = new JsonSerializer();
                    break;
            }
        }

        public async Task HandleConnection(WebSocket webSocket)
        {
            _webSocket = webSocket;
            var buffer = new byte[1024 * 4];

            await _onConnect();
            _pingPeriodicTask.Start();
            
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        _recvStringMessage(Encoding.UTF8.GetString(buffer));
                        break;
                    case WebSocketMessageType.Binary:
                        _recvBinaryMessage(buffer);
                        break;
                    case WebSocketMessageType.Close:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            _pingPeriodicTask.Stop();
            await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}