using DSLink.Nodes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DSLink.Respond
{
    public abstract class Responder
    {
        internal IDictionary<string, Action<Node>> NodeClasses;
        public readonly SubscriptionManager SubscriptionManager;
        public readonly StreamManager StreamManager;
        public readonly Node SuperRoot;

        public DSLinkContainer Link
        {
            get;
            set;
        }

        public DiskSerializer DiskSerializer
        {
            get;
            set;
        }

        public Responder(SubscriptionManager subManager, StreamManager streamManager, SuperRootNode superRoot)
        {
            NodeClasses = new Dictionary<string, Action<Node>>();
            SubscriptionManager = subManager;
            SuperRoot = superRoot;
        }

        /// <summary>
        /// Process requests incoming from the broker.
        /// </summary>
        /// <param name="requests">List of requests</param>
        /// <returns>Responses to requester</returns>
        public abstract Task<JArray> ProcessRequests(JArray requests);

        /// <summary>
        /// Adds a new node class to the responder.
        /// </summary>
        /// <param name="name">Name of the class</param>
        /// <param name="factory">Factory function for the class. First parameter is the node.</param>
        public abstract void AddNodeClass(string name, Action<Node> factory);

        public Task<bool> LoadSavedNodes()
        {
            return DiskSerializer.DeserializeFromDisk();
        }

        public Task SaveNodes()
        {
            return DiskSerializer.SerializeToDisk();
        }
    }
}
