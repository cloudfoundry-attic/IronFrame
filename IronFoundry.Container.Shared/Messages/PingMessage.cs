using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    public sealed class PingRequest : JsonRpcRequest
    {
        public static string MethodName = "Container.Ping";

        public PingRequest() : base(MethodName)
        {
        }
    }

    public sealed class PingResponse : JsonRpcResponse
    {
        public PingResponse(JToken id) : base(id)
        {
        }
    }
}
