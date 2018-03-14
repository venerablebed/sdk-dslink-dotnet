using System;
using System.Threading.Tasks;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
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

        public readonly IWindsorContainer Container;
        
        private bool _isLinkInitialized;

        public Configuration Config => Container.Resolve<Configuration>();
        public Handshake Handshake => Container.Resolve<Handshake>();
        public Responder Responder => Container.Resolve<Responder>();
        public DSLinkRequester Requester => Container.Resolve<DSLinkRequester>();
        public Connector Connector => Container.Resolve<Connector>();

        public DSLinkContainer(Configuration config)
        {
            Container = _bootstrapContainer();
            Container.Register(Component.For<DSLinkContainer>().Instance(this));
            Container.Register(Component.For<Configuration>().Instance(config));
            Container.Register(Component.For<Handshake>().ImplementedBy<Handshake>());
            Container.Register(Component.For<Connector>().ImplementedBy<WebSocketConnector>());
            
            Config._processOptions();

            if (Config.Responder)
            {
                Log.Debug("Windsor - Installing DSLink Responder");
                Container.Install(new DSLinkResponderInstaller());
            }

            if (Config.Requester)
            {
                Log.Debug("Windsor - Installing DSLink Requester");
                Container.Install(new DSLinkRequesterInstaller());
            }
            
            // Connector events
            Connector.OnMessage += OnMessage;
            Connector.OnOpen += OnOpen;
            Connector.OnClose += OnClose;
            Connector.OnFailure += OnFailure;
        }

        private static IWindsorContainer _bootstrapContainer()
        {
            return new WindsorContainer();
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
            
            await Config._initKeyPair();

            if (Config.Responder)
            {
                Responder.Init();
                
                var initDefault = true;
                if (Config.LoadNodesJson)
                {
                    initDefault = !(await Responder.LoadSavedNodes());
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

            var attemptsLeft = maxAttempts;
            uint attempts = 1;
            while (maxAttempts == 0 || attemptsLeft > 0)
            {
                var handshakeResult = await Handshake.Shake();
                if (handshakeResult != null)
                {
                    Config.RemoteEndpoint = handshakeResult;
                    await Connector.Connect();
                    return Connector.State;
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

        public async void Disconnect()
        {
            await Connector.Disconnect();
        }

        private async void OnOpen()
        {
            OnConnectionOpen();
            await Connector.Flush();
        }

        private void OnClose()
        {
            OnConnectionClosed();
            
            if (Config.Responder)
            {
                Log.Debug("Resetting responder state");
                Responder.SubscriptionManager.ClearAll();
                Responder.StreamManager.ClearAll();
            }
        }

        private async void OnFailure()
        {
            OnConnectionFailed();
            await Connect();
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

            if (message["responses"] != null)
            {
                var requests = await Requester.ProcessResponses(message["responses"].Value<JArray>());
                if (requests.Count > 0)
                {
                    response["requests"] = requests;
                }
                write = true;
            }

            if (message["requests"] != null)
            {
                var responses = await Responder.ProcessRequests(message["requests"].Value<JArray>());
                if (responses.Count > 0)
                {
                    response["responses"] = responses;
                }
                write = true;
            }

            if (write)
            {
                await Connector.Send(response);
            }
        }
    }
}
