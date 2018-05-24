using System.Threading.Tasks;
using DSLink.Util;
using Newtonsoft.Json.Linq;

namespace DSLink.Request
{
    public abstract class Requester
    {
        protected readonly IncrementingIndex RequestId;
        protected readonly RequestManager RequestManager;
        protected readonly RemoteSubscriptionManager RemoteSubManager;

        public Requester(RequestManager requestManager, RemoteSubscriptionManager remoteSubManager)
        {
            RequestId = new IncrementingIndex(1);
            RequestManager = requestManager;
            RemoteSubManager = remoteSubManager;
        }
        
        public abstract Task<JArray> ProcessResponses(JArray responses);
    }
}