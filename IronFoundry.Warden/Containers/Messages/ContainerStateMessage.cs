using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerStateRequest : JsonRpcRequest
    {
        public static string MethodName = "Container.GetContainerState";

        public ContainerStateRequest()
            : base(MethodName)
        {
        }
    }

    public class ContainerStateResponse : JsonRpcResponse<string>
    {
        public ContainerStateResponse(JToken id, string state) : base(id, state)
        {

        }
    }
}
