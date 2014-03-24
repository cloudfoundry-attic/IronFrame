using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Shared.Messaging
{
    public class ContainerStateRequest : JsonRpcRequest
    {
        public static string MethodName = "GetContainerState";

        public ContainerStateRequest()
            : base(MethodName)
        {
        }
    }

    public class ContainerStateResponse : JsonRpcResponse<string>
    {
        public ContainerStateResponse(string id, string state) : base(id, state)
        {

        }
    }
}
