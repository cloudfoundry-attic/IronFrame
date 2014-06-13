using System.Security;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerInitializeParameters
    {
        public string containerHandle;
        public string containerBaseDirectoryPath;
        public string wardenUserGroup;
    }

    public class ContainerInitializeResult
    {
        public string containerDirectoryPath;
    }

    public class ContainerInitializeRequest : JsonRpcRequest<ContainerInitializeParameters>
    {
        public static string MethodName = "Container.Initialize";
        public ContainerInitializeRequest(ContainerInitializeParameters messageParams)
            : base(MethodName)
        {
            @params = messageParams;
        }
    }

    public class ContainerInitializeResponse : JsonRpcResponse<ContainerInitializeResult>
    {
        public ContainerInitializeResponse(JToken id, string containerPath)
            : base(id, new ContainerInitializeResult { containerDirectoryPath = containerPath })
        {
        }
    }
}
