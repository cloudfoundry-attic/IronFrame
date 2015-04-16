using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    internal class PingRequest : JsonRpcRequest
    {
        public static string MethodName = "Container.Ping";

        public PingRequest() : base(MethodName)
        {
        }
    }

    internal class PingResponse : JsonRpcResponse
    {
        public PingResponse(JToken id) : base(id)
        {
        }
    }
}
