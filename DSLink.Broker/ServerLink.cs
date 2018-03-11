using System;
using DSLink.Util;
using DSLink.Broker.Objects;
using DSLink.Connection;

namespace DSLink.Broker
{
    public class ServerLink
    {
        private static readonly Random Random = new Random();
        private ConnRequestObject _requestObject;

        public readonly string DsId;
        public readonly KeyPair TempKey;
        public bool IsResponder => _requestObject.isResponder;
        public bool IsRequester => _requestObject.isRequester;
        public string NextSalt
        {
            get
            {
                var bytes = new byte[16];
                Random.NextBytes(bytes);
                return BitConverter.ToString(SHA256.ComputeHash(bytes));
            }
        }

        public ServerLink(string dsId)
        {
            DsId = dsId;
            TempKey = new KeyPair();
        }

        public void SetRequestObject(ConnRequestObject requestObject)
        {
            _requestObject = requestObject;
        }
    }
}