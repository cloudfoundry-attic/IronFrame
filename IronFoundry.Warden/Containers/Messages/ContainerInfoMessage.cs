using IronFoundry.Container;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerInfoRequest : JsonRpcRequest
    {
        public const string MethodName = "Container.Info";

        public ContainerInfoRequest() : base(MethodName)
        {
        }
    }

    public class ContainerInfoResponse : JsonRpcResponse<ContainerInfo>
    {
        public ContainerInfoResponse(JToken id, ContainerInfo result) : base(id, result)
        {
        }
    }
}
