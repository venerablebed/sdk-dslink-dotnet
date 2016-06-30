﻿using System;
using System.Collections.Generic;
using DSLink.Connection.Serializer;
using DSLink.Container;
using DSLink.Nodes;
using DSLink.Nodes.Actions;
using DSLink.Respond;

namespace DSLink.Request
{
    /// <summary>
    /// Base request object.
    /// </summary>
    public abstract class BaseRequest
    {
        /// <summary>
        /// Request identifier for the request.
        /// </summary>
        public int RequestID
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DSLink.Request.BaseRequest"/> class.
        /// </summary>
        /// <param name="requestID">Request identifier</param>
        protected BaseRequest(int requestID)
        {
            RequestID = requestID;
        }

        /// <summary>
        /// Request method.
        /// </summary>
        public abstract string Method();

        /// <summary>
        /// Serialize the request.
        /// </summary>
        public virtual RequestObject Serialize()
        {
            return new RequestObject()
            {
                rid = RequestID,
                method = Method()
            };
        }
    }

    /// <summary>
    /// List request object.
    /// </summary>
    public class ListRequest : BaseRequest
    {
        /// <summary>
        /// Callback that is called when response is received.
        /// </summary>
        public readonly Action<ListResponse> Callback;

        /// <summary>
        /// Path of the request.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DSLink.Request.ListRequest"/> class.
        /// </summary>
        /// <param name="requestID">Request identifier</param>
        /// <param name="callback">Callback</param>
        /// <param name="path">Path</param>
        public ListRequest(int requestID, Action<ListResponse> callback, string path) : base(requestID)
        {
            Callback = callback;
            Path = path;
        }

        /// <summary>
        /// Request method.
        /// </summary>
        public override string Method() => "list";

        /// <summary>
        /// Serialize the request.
        /// </summary>
        public override RequestObject Serialize()
        {
            var baseSerialized = base.Serialize();
            baseSerialized.path = Path;
            return baseSerialized;
        }
    }

    /// <summary>
    /// Set request object.
    /// </summary>
    public class SetRequest : BaseRequest
    {
        /// <summary>
        /// Path to run the request with.
        /// </summary>
        public readonly string Path;

        public readonly Permission Permission;

        public readonly Value Value;

        public SetRequest(int requestID, string path, Permission permission, Value value) : base(requestID)
        {
            Path = path;
            Permission = permission;
            Value = value;
        }

        public override string Method() => "set";

        public override RequestObject Serialize()
        {
            var baseSerialized = base.Serialize();
            baseSerialized.path = Path;
            baseSerialized.permit = Permission.ToString();
            baseSerialized.value = Value.Get();
            return baseSerialized;
        }
    }

    public class RemoveRequest : BaseRequest
    {
        public string Path
        {
            get;
        }

        public RemoveRequest(int requestID, string path) : base(requestID)
        {
            Path = path;
        }

        public override string Method() => "remove";

        public override RequestObject Serialize()
        {
            var baseSerialized = base.Serialize();
            baseSerialized.path = Path;
            return baseSerialized;
        }
    }

    public class InvokeRequest : BaseRequest
    {
        public readonly string Path;

        public readonly Permission Permission;

        public readonly Dictionary<string, dynamic> Parameters;

        public readonly Action<InvokeResponse> Callback;

        private readonly AbstractContainer _link;

        private readonly List<Column> _columns;

        private bool _firstUpdate = true;

        public InvokeRequest(int requestID, string path, Permission permission, Dictionary<string, dynamic> parameters,
                             Action<InvokeResponse> callback = null, AbstractContainer link = null, List<Column> columns = null) : base(requestID)
        {
            Path = path;
            Permission = permission;
            Parameters = parameters;
            Callback = callback;
            _link = link;
            _columns = columns;
        }

        public override string Method() => "invoke";

        public override RequestObject Serialize()
        {
            var baseSerialized = base.Serialize();
            baseSerialized.path = Path;
            if (baseSerialized.permit != null)
            {
                baseSerialized.permit = Permission.ToString();
            }
            baseSerialized.@params = Parameters;
            return baseSerialized;
        }

        public void SendUpdates(List<dynamic> updates, bool close = false)
        {
            if (_link == null || _columns == null)
            {
                throw new NotSupportedException("Link and columns are null, cannot send updates");
            }
            var updateRootObject = new RootObject
            {
                responses = new List<ResponseObject>
                {
                    new ResponseObject
                    {
                        rid = RequestID,
                        stream = (close ? "close" : "open"),
                        updates = updates
                    }
                }
            };
            if (_firstUpdate)
            {
                _firstUpdate = false;
                updateRootObject.responses[0].meta = new Dictionary<string, dynamic>
                {
                    {"mode", "append"},
                    {"meta", new Dictionary<string, dynamic>()}
                };
                updateRootObject.responses[0].columns = _columns;
            }
            _link.Connector.Write(updateRootObject);
        }

        public void Close()
        {
            if (_link == null)
            {
                throw new NotSupportedException("Link is null, cannot send updates");
            }
            _link.Connector.Write(new RootObject
            {
                responses = new List<ResponseObject>
                {
                    new ResponseObject
                    {
                        rid = RequestID,
                        stream = "closed"
                    }
                }
            });
        }
    }
}
