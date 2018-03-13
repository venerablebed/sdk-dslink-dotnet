using System;
using System.Threading.Tasks;
using DSLink.Connection;
using DSLink.Logger;
using Newtonsoft.Json.Linq;
using DSLink.Request;
using DSLink.Respond;
using DSLink.Util;

namespace DSLink
{
    public class DSLinkContainer
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        private readonly PeriodicTask _pingPeriodicTask;
        private Handshake _handshake;
        private bool _reconnectOnFailure;
        private bool _isLinkInitialized;
        private readonly Configuration _config;
        private readonly DSLinkResponder _responder;
        private readonly DSLinkRequester _requester;
        private readonly Connector _connector;

        public Configuration Config => _config;
        public virtual Responder Responder => _responder;
        public virtual DSLinkRequester Requester => _requester;
        public virtual Connector Connector => _connector;

        public DSLinkContainer(Configuration config)
        {
            _pingPeriodicTask = new PeriodicTask(OnPingTaskElapsed, 30000);
            _config = config;
            _config._processOptions();
            _reconnectOnFailure = true;
            _connector = new WebSocketConnector(_config);

            if (Config.Responder)
            {
                _responder = new DSLinkResponder(this);
            }
            if (Config.Requester)
            {
                _requester = new DSLinkRequester(this);
            }

            // Connector events
            _connector.OnMessage += OnMessage;
            _connector.OnBinaryMessage += OnMessage;
            _connector.OnOpen += OnOpen;
            _connector.OnClose += OnClose;
        }

        /// <summary>
        /// Initializes the DSLink's node structure by building, or
        /// loading from disk when the link has been ran before.
        /// </summary>
        public async Task Initialize()
        {
            if (_isLinkInitialized)
            {
                return;
            }
            _isLinkInitialized = true;
            
            await _config._initKeyPair();

            _responder?.Init();
            _requester?.Init();

            if (Config.Responder)
            {
                var initDefault = true;
                if (Config.LoadNodesJson)
                {
                    initDefault = !(await LoadSavedNodes());
                }
                if (initDefault)
                {
                    InitializeDefaultNodes();
                }
            }
        }

        /// <summary>
        /// Used to initialize the node structure when nodes.json does not
        /// exist yet or failed to load.
        /// </summary>
        public virtual void InitializeDefaultNodes()
        {}

        public async Task<ConnectionState> Connect(uint maxAttempts = 0)
        {
            await Initialize();

            _reconnectOnFailure = true;
            _handshake = new Handshake(this);
            var attemptsLeft = maxAttempts;
            uint attempts = 1;
            while (maxAttempts == 0 || attemptsLeft > 0)
            {
                var handshakeResult = await _handshake.Shake();
                if (handshakeResult != null)
                {
                    _config.RemoteEndpoint = handshakeResult;
                    await Connector.Connect();
                    return Connector.ConnectionState;
                }

                var delay = attempts;
                if (delay > Config.MaxConnectionCooldown)
                {
                    delay = Config.MaxConnectionCooldown;
                }
                Log.Warning($"Failed to connect, delaying for {delay} seconds");
                await Task.Delay(TimeSpan.FromSeconds(delay));

                if (attemptsLeft > 0)
                {
                    attemptsLeft--;
                }
                attempts++;
            }
            Log.Warning("Failed to connect within the allotted connection attempt limit.");
            OnConnectionFailed();
            return ConnectionState.Disconnected;
        }

        public void Disconnect()
        {
            _reconnectOnFailure = false;
            Connector.Disconnect();
        }

        public async Task<bool> LoadSavedNodes()
        {
            if (_responder == null)
            {
                throw new DSAException(this, "Responder is not enabled.");
            }
            
            return await Responder.DiskSerializer.DeserializeFromDisk();
        }

        public async Task SaveNodes()
        {
            if (_responder == null)
            {
                throw new DSAException(this, "Responder is not enabled.");
            }
            
            await Responder.DiskSerializer.SerializeToDisk();
        }

        private async void OnOpen()
        {
            OnConnectionOpen();
            _pingPeriodicTask.Start();
            await Connector.Flush();
        }

        private async void OnClose()
        {
            OnConnectionClosed();
            _pingPeriodicTask.Stop();
            if (Responder != null)
            {
                Responder.SubscriptionManager.ClearAll();
                Responder.StreamManager.ClearAll();
            }

            if (_reconnectOnFailure)
            {
                await Connect();
            }
        }

        /// <summary>
        /// Called when the connection is opened to the broker.
        /// Override when you need to perform an action after connection opens.
        /// </summary>
        protected virtual void OnConnectionOpen() {}

        /// <summary>
        /// Called when the connection is closed to the broker.
        /// Override when you need to perform an action after connection closes.
        /// </summary>
        protected virtual void OnConnectionClosed() {}

        /// <summary>
        /// Called when the connection fails to connect to the broker.
        /// Override when you need to perform an action after failure to connect.
        /// </summary>
        protected virtual void OnConnectionFailed() {}

        private async void OnMessage(JObject message)
        {
            var response = new JObject();
            if (message["msg"] != null)
            {
                response["ack"] = message["msg"].Value<int>();
            }

            var write = false;

            if (message["requests"] != null)
            {
                var responses = await Responder.ProcessRequests(message["requests"].Value<JArray>());
                if (responses.Count > 0)
                {
                    response["responses"] = responses;
                }
                write = true;
            }

            if (message["responses"] != null)
            {
                var requests = await Requester.ProcessResponses(message["responses"].Value<JArray>());
                if (requests.Count > 0)
                {
                    response["requests"] = requests;
                }
                write = true;
            }

            if (write)
            {
                await Connector.Write(response);
            }
        }

        private async void OnPingTaskElapsed()
        {
            if (Connector.Connected())
            {
                // Write a blank message containing no responses/requests.
                await Connector.Write(new JObject(), false);
            }
        }
    }
}
