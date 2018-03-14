using DSLink.Connection;
using DSLink.Util;

namespace DSLink.Request
{
    public class Requester
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
    }
}