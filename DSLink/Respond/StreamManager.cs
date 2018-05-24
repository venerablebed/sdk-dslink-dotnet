using System.Collections.Generic;
using DSLink.Logger;
using DSLink.Nodes;

namespace DSLink.Respond
{
    public class StreamManager
    {
        private static readonly BaseLogger Log = LogManager.GetLogger();
        
        private readonly Dictionary<int, string> _requestIdToPath = new Dictionary<int, string>();
        private readonly Responder _responder;

        public StreamManager(Responder responder)
        {
            _responder = responder;
        }

        public void OpenStream(int requestId, Node node)
        {
            _requestIdToPath.Add(requestId, node.Path);
            lock (node._streams)
            {
                node._streams.Add(requestId);
            }
        }

        public void OpenStreamLater(int requestId, string path)
        {
            _requestIdToPath.Add(requestId, path);
        }

        public void CloseStream(int requestId)
        {
            try
            {
                var node = _responder.SuperRoot.Get(_requestIdToPath[requestId]);
                if (node != null)
                {
                    lock (node._streams)
                    {
                        node._streams.Remove(requestId);
                    }
                }
                _requestIdToPath.Remove(requestId);
            }
            catch (KeyNotFoundException)
            {
                Log.Debug($"Failed to Close: unknown request id or node for {requestId}");
            }
        }

        public void OnActivateNode(Node node)
        {
            foreach (var id in _requestIdToPath.Keys)
            {
                var path = _requestIdToPath[id];
                if (path != node.Path) continue;
                lock (node._streams)
                {
                    node._streams.Add(id);
                }
            }
        }
                 
        internal void ClearAll()
        {
            _requestIdToPath.Clear();
        }
    }
}
