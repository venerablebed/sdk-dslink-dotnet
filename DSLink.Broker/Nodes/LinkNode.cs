using DSLink.Nodes;

namespace DSLink.Broker.Nodes
{
    public class LinkNode : Node
    {
        public LinkNode(string name, Node parent) : base(name, parent, null)
        {
        }
    }
}