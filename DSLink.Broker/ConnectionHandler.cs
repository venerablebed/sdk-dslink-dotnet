using DSLink.Broker.Objects;

namespace DSLink.Broker
{
    public class ConnectionHandler
    {
        public ServerLink GetLink(string dsId)
        {
            return null;
        }
        
        public void AddLink(ServerLink link)
        {
        }

        public ConnResponseObject InitLink(ServerLink link)
        {
            return new ConnResponseObject();
        }
    }
}
