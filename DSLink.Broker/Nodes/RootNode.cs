using DSLink.Nodes;

namespace DSLink.Broker.Nodes
{
    public class RootNode : Node
    {
        public readonly DownstreamNode Downstream;

        public RootNode() : base("", null)
        {
            Downstream = new DownstreamNode(this);
            AddChild(Downstream);
        }
    }
}