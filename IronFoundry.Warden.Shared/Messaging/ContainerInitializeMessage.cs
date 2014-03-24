using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Shared.Messaging
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
        public ContainerInitializeResponse(string id) : base(id, true)
        {
        }
    }
}
