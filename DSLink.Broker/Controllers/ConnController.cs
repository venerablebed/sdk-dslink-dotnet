using DSLink.Broker.Objects;
using DSLink.Util;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DSLink.Broker.Controllers
{
    [Route("conn")]
    public class ConnController : Controller
    {
        [HttpGet]
        public string Get()
        {
            return "/conn is not meant to be loaded in a browser";
        }

        [HttpPost]
        public JObject Post([FromBody] ConnRequestObject requestObject, [FromQuery(Name = "dsId")] string dsId,
            [FromQuery(Name = "token")] string token)
        {
            var error = "";

            if (string.IsNullOrEmpty(requestObject.publicKey))
            {
                error = "publicKey must not be null or empty.";
                goto error;
            }
            
            if (!requestObject.isResponder && !requestObject.isRequester)
            {
                error = "DSLink must be one or both of responder or requester.";
                goto error;
            }

            if (string.IsNullOrEmpty(requestObject.version))
            {
                error = "DSLink must provide DSA version";
                goto error;
            }

            if (dsId == null || dsId.Length < 43)
            {
                error = "dsId is null or invalid (under 43 characters in length).";
                goto error;
            }
            
            // Retrieve pre-existing link
            var link = Program.Broker.ConnectionHandler.GetLink(dsId);

            if (link == null)
            {
                var pkBytes = UrlBase64.Decode(requestObject.publicKey);
                // TODO: Validate public key bytes

                link = new ServerLink(dsId);

                Program.Broker.ConnectionHandler.AddLink(link);
            }
            
            return new JObject();
            
            error:
            return new JObject
            {
                new JProperty("error", error)
            };
        }
    }
}
