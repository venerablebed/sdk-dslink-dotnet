using System.Collections.Generic;

namespace DSLink.Broker.Objects
{
    public class ConnRequestObject
    {
        public string publicKey;
        public bool isResponder;
        public bool isRequester;
        public string version;
        public List<string> formats;
        public bool enableWebSocketCompression;
    }

    public class ConnResponseObject
    {
        public string dsId;
        public string publicKey;
        public string wsUri;
        public string httpUri;
        public string version;
        public string tempKey;
        public string salt;
        public string saltS;
        public string saltL;
        public string format;
        public string path;
        public string error;
    }
}