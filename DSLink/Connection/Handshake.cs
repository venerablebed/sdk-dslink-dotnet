using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSLink.Logger;
using DSLink.Serializer;
using DSLink.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DSLink.Connection
{
    public class Handshake
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        /// <summary>
        /// DSA Version.
        /// </summary>
        private const string _dsaVersion = "1.1.2";

        /// <summary>
        /// Instance of link container.
        /// </summary>
        private readonly DSLinkContainer _link;

        /// <summary>
        /// HttpClient for handshaking.
        /// </summary>
        private readonly HttpClient _httpClient;

        public Handshake(DSLinkContainer link)
        {
            _link = link;
            _httpClient = new HttpClient();
        }

        private string _buildUrl()
        {
            var url = _link.Config.BrokerUrl;
            url += "?dsId=" + _link.Config.DsId;
            if (_link.Config.HasToken)
            {
                url += "&token=" + _link.Config.TokenParameter;
            }
            return url;
        }

        /// <summary>
        /// Run handshake with the server.
        /// </summary>
        public async Task<RemoteEndpoint> Shake()
        {
            Log.Debug("Handshaking with " + _buildUrl());
            HttpResponseMessage resp = null;
            try
            {
                resp = await RunHandshake();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            if (resp == null)
            {
                Log.Error("Handshake returned null.");
                return null;
            }
            
            Log.Debug("Handshake status code: " + resp.StatusCode.ToString());

            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            Log.Debug("Handshake successful");
            var bodyString = await resp.Content.ReadAsStringAsync();
            Log.Debug("Handshake response: " + bodyString);
            
            return JsonConvert.DeserializeObject<RemoteEndpoint>(
                bodyString
            );
        }

        /// <summary>
        /// Performs handshake with POST endpoint on the broker.
        /// </summary>
        private Task<HttpResponseMessage> RunHandshake()
        {
            return _httpClient.PostAsync(_buildUrl(), 
                new StringContent(GetJson().ToString()));
        }

        /// <summary>
        /// Creates a JSON object with necessary data for handshake.
        /// </summary>
        /// <returns>JObject with necessary data</returns>
        private JObject GetJson()
        {
            return new JObject
            {
                {"publicKey", UrlBase64.Encode(_link.Config.KeyPair.EncodedPublicKey)},
                {"isRequester", _link.Config.Requester},
                {"isResponder", _link.Config.Responder},
                {"linkData", new JObject()},
                {"version", _dsaVersion},
                {"formats", new JArray(Serializers.Types.Keys.ToList())},
                {"enableWebSocketCompression", _link.Connector.SupportsCompression}
            };
        }
    }

    /// <summary>
    /// Data received from the handshake's body content.
    /// </summary>
    public class RemoteEndpoint
    {
        /// <summary>
        /// DS Identifier of the broker.
        /// </summary>
        public string dsId;

        /// <summary>
        /// Public key of the server.
        /// </summary>
        public string publicKey;

        /// <summary>
        /// WebSocket URI endpoint.
        /// </summary>
        public string wsUri;

        /// <summary>
        /// HTTP handshake endpoint.
        /// </summary>
        public string httpUri;

        /// <summary>
        /// Temporary key for handshake.
        /// </summary>
        public string tempKey;

        /// <summary>
        /// Salt for the handshake.
        /// </summary>
        public string salt;

        /// <summary>
        /// Path of this link.
        /// </summary>
        public string path;

        /// <summary>
        /// Version of DSA the broker is running.
        /// </summary>
        public string version;

        /// <summary>
        /// Update interval.
        /// </summary>
        public int updateInterval;

        /// <summary>
        /// Serialization format used to communicate with.
        /// </summary>
        public string format = "json";
    }
}
