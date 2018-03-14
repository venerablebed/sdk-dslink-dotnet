using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DSLink.Logger;
using DSLink.Serializer;
using DSLink.Util;

namespace DSLink.Connection
{
    public abstract class Connector
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();

        private readonly Configuration _config;
        private readonly IncrementingIndex _msgId;

        public virtual BaseSerializer Serializer { get; private set; }

        public ConnectionState State { get; private set; }

        /// <summary>
        /// Queue object for queueing up data when the WebSocket is either closed
        /// or we want to send a large amount of data in one burst. When set to
        /// false, the flush method is automatically called.
        /// </summary>
        private JObject _queue;
        
        /// <summary>
        /// A PeriodicTask that is used to occasionally (usually thirty seconds)
        /// send an empty message to keep the connection alive.
        /// </summary>
        private readonly PeriodicTask _pingPeriodicTask;

        /// <summary>
        /// Queue lock object.
        /// </summary>
        private readonly object _queueLock = new object();

        /// <summary>
        /// Whether we should enable the queueing of messages.
        /// </summary>
        private bool _enableQueue = true;

        /// <summary>
        /// Do we have a queue flush event scheduled?
        /// </summary>
        private bool _hasQueueEvent;

        /// <summary>
        /// Subscription value queue.
        /// </summary>
        private JArray _subscriptionValueQueue = new JArray();

        /// <summary>
        /// Whether we should enable the queueing of messages.
        /// </summary>
        public bool EnableQueue
        {
            set
            {
                _enableQueue = value;
                if (!value)
                {
#pragma warning disable CS4014
                    Flush();
#pragma warning restore CS4014
                }
            }
            get => _enableQueue;
        }

        /// <summary>
        /// True if the WebSocket implementation supports compression.
        /// </summary>
        public virtual bool SupportsCompression => false;

        /// <summary>
        /// Event that is triggered when the connection is opened.
        /// </summary>
        public event Action OnOpen;

        /// <summary>
        /// Event that is triggered when the connection is closed.
        /// </summary>
        public event Action OnClose;

        /// <summary>
        /// Event that is triggered upon connection failure.
        /// </summary>
        public event Action OnFailure;

        /// <summary>
        /// Event occurs when String data is received.
        /// </summary>
        public event Action<JObject> OnMessage;

        protected Connector(Configuration config)
        {
            _config = config;
            _pingPeriodicTask = new PeriodicTask(_onPingTaskElapsed, 30000);
            State = ConnectionState.Disconnected;
            _msgId = new IncrementingIndex();

            OnFailure += () =>
            {
                State = ConnectionState.Failure;
                Log.Error("Connection failure");
            };
        }

        /// <summary>
        /// Rewrites the broker endpoint into the websocket connection endpoint.
        /// </summary>
        protected string WsUrl
        {
            get
            {
                var uri = new Uri(_config.BrokerUrl);
                var sb = new StringBuilder();

                sb.Append(uri.Scheme.Equals("https") ? "wss://" : "ws://");
                sb.Append(uri.Host).Append(":").Append(uri.Port).Append(_config.RemoteEndpoint.wsUri);
                sb.Append("?");
                sb.Append("dsId=").Append(_config.DsId);
                sb.Append("&auth=").Append(_config.Authentication);
                sb.Append("&format=").Append(_config.CommunicationFormatUsed);
                if (_config.HasToken)
                {
                    sb.Append("&token=").Append(_config.TokenParameter);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Connect to the broker.
        /// </summary>
        public async Task Connect()
        {
            Serializer = (BaseSerializer) Activator.CreateInstance(
                Serializers.Types[_config.CommunicationFormatUsed]
            );
            State = ConnectionState.Connecting;
            Log.Info("Connecting");
            Log.Debug($"Connecting to {WsUrl}");

            State = await Open();

            if (State == ConnectionState.Connected)
            {
                _pingPeriodicTask.Start();
                OnOpen?.Invoke();
            }
            else
            {
                OnFailure?.Invoke();
            }
        }

        /// <summary>
        /// Disconnect from the broker.
        /// </summary>
        public async Task Disconnect()
        {
            State = ConnectionState.Disconnecting;
            Log.Info("Disconnecting");

            State = await Close();

            if (State == ConnectionState.Disconnected)
            {
                _pingPeriodicTask.Stop();
                OnClose?.Invoke();
            }
            else
            {
                Log.Error("Connection did not properly disconnect");
            }
        }

        protected abstract Task<ConnectionState> Open();
        protected abstract Task<ConnectionState> Close();
        protected abstract Task Write(string data);
        protected abstract Task Write(byte[] data);

        /// <summary>
        /// Write the specified data.
        /// </summary>
        /// <param name="data">RootObject to serialize and send</param>
        /// <param name="allowQueue">Whether to allow the data to be added to the queue</param>
        public async Task Send(JObject data, bool allowQueue = true)
        {
            if ((State != ConnectionState.Connected || EnableQueue) && allowQueue)
            {
                lock (_queueLock)
                {
                    if (_queue == null)
                    {
                        _queue = new JObject
                        {
                            new JProperty("msg", _msgId.Next),
                            new JProperty("responses", new JArray()),
                            new JProperty("requests", new JArray())
                        };
                    }

                    if (data["responses"] != null)
                    {
                        foreach (var resp in data["responses"].Value<JArray>())
                        {
                            ((JArray) _queue["responses"]).Add(resp);
                        }
                    }

                    if (data["requests"] != null)
                    {
                        foreach (var req in data["requests"].Value<JArray>())
                        {
                            ((JArray) _queue["requests"]).Add(req);
                        }
                    }

                    if (data["ack"] != null)
                    {
                        _queue["ack"] = data["ack"];
                    }

                    if (!_hasQueueEvent)
                    {
                        // Set flag to queue flush
                        _hasQueueEvent = true;
                    }
                }

                if (_hasQueueEvent)
                {
                    await TriggerQueueFlush();
                }

                return;
            }

            if (data["msg"] == null)
            {
                data["msg"] = _msgId.Next;
            }

            if (data["requests"] != null && data["requests"].Value<JArray>().Count == 0)
            {
                data.Remove("requests");
            }

            if (data["responses"] != null && data["responses"].Value<JArray>().Count == 0)
            {
                data.Remove("responses");
            }

            var serialized = Serializer.Serialize(data);
            switch (serialized)
            {
                case string _:
                    SendData(serialized);
                    break;
                case byte[] _:
                    SendData(serialized);
                    break;
                default:
                    throw new FormatException($"Cannot send message of type {serialized.Type}");
            }
        }

        /// <summary>
        /// Called to add value updates.
        /// </summary>
        /// <param name="update">Value Update</param>
        public virtual async Task AddValueUpdateResponse(JToken update)
        {
            if (EnableQueue)
            {
                lock (_queueLock)
                {
                    _subscriptionValueQueue.Add(update);

                    if (!_hasQueueEvent)
                    {
                        _hasQueueEvent = true;
                        // Schedule event for queue flush.
                        Task.Run((() => TriggerQueueFlush()));
                    }
                }
            }
            else
            {
                var response = new JObject
                {
                    new JProperty("rid", 0),
                    new JProperty("updates", new JArray { update })
                };

                await Send(new JObject
                {
                    new JProperty("responses", new JArray { response })
                });
            }
        }

        /// <summary>
        /// Writes a string to the connector.
        /// </summary>
        /// <param name="data">String to write</param>
        private Task SendData(string data)
        {
            LogMessageString(true, data);
            return Write(data);
        }

        /// <summary>
        /// Writes binary to the connector.
        /// </summary>
        /// <param name="data">Binary to write</param>
        private Task SendData(byte[] data)
        {
            LogMessageBytes(true, data);
            return Write(data);
        }

        protected void EmitFailure()
        {
            OnFailure?.Invoke();
        }

        protected void Receive(string data)
        {
            LogMessageString(false, data);
            OnMessage?.Invoke(Serializer.Deserialize(data));
        }
        
        protected void Receive(byte[] data)
        {
            LogMessageBytes(false, data);
            OnMessage?.Invoke(Serializer.Deserialize(data));
        }

        internal async Task Flush(bool fromEvent = false)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            Log.Debug("Flushing connection message queue");
            JObject queueToFlush = null;
            lock (_queueLock)
            {
                if (fromEvent)
                {
                    _hasQueueEvent = false;
                }

                if (_subscriptionValueQueue.Count != 0)
                {
                    var response = new JObject
                    {
                        {"rid", 0},
                        {"updates", _subscriptionValueQueue}
                    };

                    if (_queue == null)
                    {
                        _queue = new JObject
                        {
                            {"responses", new JArray()}
                        };
                    }

                    _queue["responses"].Value<JArray>().Add(response);
                }

                if (_queue != null)
                {
                    queueToFlush = _queue;
                    _queue = null;
                }

                if (_subscriptionValueQueue.Count != 0)
                {
                    _subscriptionValueQueue = new JArray();
                }
            }

            if (queueToFlush != null)
            {
                await Send(queueToFlush, false);
            }
        }

        /// <summary>
        /// Flushes the queue for scheduled queue events.
        /// </summary>
        internal async Task TriggerQueueFlush()
        {
            await Flush(true);
        }

        private static void LogMessageString(bool sent, string data)
        {
            if (Log.ToPrint.DoesPrint(LogLevel.Debug))
            {
                var verb = sent ? "Sent" : "Received";
                var logString = $"Text {verb}: {data}";
                Log.Debug(logString);
            }
        }

        private static void LogMessageBytes(bool sent, byte[] data)
        {
            if (Log.ToPrint.DoesPrint(LogLevel.Debug))
            {
                var verb = sent ? "Sent" : "Received";
                var logString = $"Binary {verb}: ";
                if (data.Length < 5000)
                {
                    logString += BitConverter.ToString(data);
                }
                else
                {
                    logString += "(over 5000 bytes)";
                }

                Log.Debug(logString);
            }
        }
        
        private async void _onPingTaskElapsed()
        {
            if (State == ConnectionState.Connected)
            {
                // Write a blank message containing no responses/requests.
                // Disable the queue for this specific message.
                await Send(new JObject(), false);
            }
        }
    }
}