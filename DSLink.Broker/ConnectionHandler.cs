using System.Collections.Generic;
using DSLink.Broker.Objects;
using DSLink.Util;

namespace DSLink.Broker
{
    public class ConnectionHandler
    {
        private readonly Dictionary<string, ServerLink> _links;

        public ConnectionHandler()
        {
            _links = new Dictionary<string, ServerLink>();
        }
        
        public ServerLink GetLink(string dsId)
        {
            _links.TryGetValue(dsId, out var link);
            return link;
        }
        
        public void AddLink(ServerLink link)
        {
            _links.Add(link.DsId, link);
        }

        public ConnResponseObject InitLink(ServerLink link)
        {
            link.TempKey.Generate();
            
            return new ConnResponseObject
            {
                dsId = Program.Broker.DsId,
                publicKey = UrlBase64.Encode(Program.Broker.KeyPair.EncodedPublicKey),
                wsUri = "/ws",
                version = "1.1.2",
                tempKey = UrlBase64.Encode(link.TempKey.EncodedPublicKey),
                format = "json", // TODO: Properly handle formats
                path = "/downstream/" + link.DsId, // TODO: Properly handle path
                salt = link.NextSalt
            };
        }
    }
}
