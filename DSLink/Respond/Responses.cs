using System.Threading.Tasks;
using DSLink.Connection;
using DSLink.Nodes;
using DSLink.Request;
using Newtonsoft.Json.Linq;

namespace DSLink.Respond
{
    public class Response
    {
        protected readonly Connector Connector;
        protected readonly RequestManager RequestManager;
        
        public int RequestId
        {
            get;
        }

        public Response(Connector connector, RequestManager requestManager, int requestId)
        {
            Connector = connector;
            RequestManager = requestManager;
            RequestId = requestId;
        }

        /// <summary>
        /// Close the request.
        /// </summary>
        public async Task Close()
        {
            RequestManager.StopRequest(RequestId);
            await Connector.Send(new JObject
            {
                new JProperty("responses", new JObject
                {
                    new JObject
                    {
                        new JProperty("rid", RequestId),
                        new JProperty("stream", "closed")
                    }
                })
            });
        }
    }

    public class ListResponse : Response
    {
        /// <summary>
        /// Path of the list request.
        /// </summary>
        public string Path
        {
            get;
            protected set;
        }

        /// <summary>
        /// Node of the list request.
        /// </summary>
        public Node Node
        {
            get;
        }

        public ListResponse(Connector connector, RequestManager requestManager,
            int requestId, string path, Node node)
            : base(connector, requestManager, requestId)
        {
            Path = path;
            Node = node;
        }
    }

    public class InvokeResponse : Response
    {
        /// <summary>
        /// Path of the Node.
        /// </summary>
        public string Path
        {
            get;
        }

        /// <summary>
        /// Columns from Response.
        /// </summary>
        public JArray Columns
        {
            get;
        }

        /// <summary>
        /// Updates from Response.
        /// </summary>
        public JArray Updates
        {
            get;
        }

        /// <summary>
        /// True when Columns is neither true or 0.
        /// </summary>
        public bool HasColumns => Columns != null && Columns.Count > 0;

        /// <summary>
        /// True when Updates is neither true or 0;
        /// </summary>
        public bool HasUpdates => Updates.Count > 0;

        public InvokeResponse(Connector connector, RequestManager requestManager, int requestID,
            string path, JArray columns, JArray updates)
            : base(connector, requestManager, requestID)
        {
            Path = path;
            Columns = columns;
            Updates = updates;
        }
    }
}
