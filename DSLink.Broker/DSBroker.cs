using DSLink.Broker.Nodes;
using DSLink.Connection;

namespace DSLink.Broker
{
    public class DSBroker
    {
        public readonly KeyPair KeyPair;
        public readonly RootNode RootNode;
        public readonly ConnectionHandler ConnectionHandler;
        public readonly TokenHandler TokenHandler;

        // TODO: Replace `dotNETBroker` with configurable name.
        public string DsId => "dotNETBroker-" + KeyPair.GenerateIdSuffix();
        
        public DSBroker()
        {
            KeyPair = new KeyPair();
            KeyPair.Generate();
            
            RootNode = new RootNode();
            
            ConnectionHandler = new ConnectionHandler();
            TokenHandler = new TokenHandler();
        }
    }
}