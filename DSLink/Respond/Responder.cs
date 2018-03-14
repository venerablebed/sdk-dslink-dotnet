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

        public DSLinkContainer Link
        {
            get;
            set;
        }

        public SuperRootNode SuperRoot
        {
            get;
            set;
        }

        public SubscriptionManager SubscriptionManager
        {
            get;
            set;
        }

        public StreamManager StreamManager
        {
            get;
            set;
        }

        public DiskSerializer DiskSerializer
        {
            get;
            set;
        }

        public Responder()
        {
            NodeClasses = new Dictionary<string, Action<Node>>();
        }

        public abstract void Init();

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

        public async Task<bool> LoadSavedNodes(DSLinkContainer dsLinkContainer)
        {
            return await DiskSerializer.DeserializeFromDisk();
        }

        public async Task SaveNodes()
        {
            await DiskSerializer.SerializeToDisk();
        }
    }
}
