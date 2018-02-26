namespace DSLink.Broker
{
    public class DSBroker
    {
        public readonly ConnectionHandler ConnectionHandler;
        public readonly TokenHandler TokenHandler;
        
        public DSBroker()
        {
            ConnectionHandler = new ConnectionHandler();
            TokenHandler = new TokenHandler();
        }
    }
}