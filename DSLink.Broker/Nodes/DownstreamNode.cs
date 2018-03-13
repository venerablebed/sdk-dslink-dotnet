using DSLink.Nodes;

namespace DSLink.Broker.Nodes
{
    public class DownstreamNode : Node
    {
        public DownstreamNode(Node parent) : base("downstream", parent)
        {
        }
        
        public void AddLink(ServerLink link)
        {
        }
    }
}