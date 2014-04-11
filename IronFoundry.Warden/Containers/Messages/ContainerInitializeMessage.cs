using System.Security;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerInitializeParameters
    {
        public string containerHandle;
        public string containerDirectoryPath;
        public string userName;

        [Newtonsoft.Json.JsonConverter(typeof(SecureStringJsonConverter))]
        public SecureString userPassword;
    }

    public class ContainerInitializeRequest : JsonRpcRequest<ContainerInitializeParameters>
    {
        public static string MethodName = "ContainerInitialize";
        public ContainerInitializeRequest(ContainerInitializeParameters messageParams) : base(MethodName)
        {
            @params = messageParams;
        }
    }

    public class ContainerInitializeResponse : JsonRpcResponse<bool>
    {
        public ContainerInitializeResponse(JToken id) : base(id, true)
        {
        }
    }
}
