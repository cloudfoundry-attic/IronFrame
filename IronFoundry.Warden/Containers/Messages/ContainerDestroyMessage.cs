using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerDestroyRequest : JsonRpcRequest
    {
        public static string MethodName = "ContainerDestroy";

        public ContainerDestroyRequest()
            : base(MethodName)
        {

        }
    }

    public class ContainerDestroyResponse : JsonRpcResponse<bool>
    {
        public ContainerDestroyResponse(JToken id, bool result = true)
            : base(id, result)
        {
        }
    }
}
