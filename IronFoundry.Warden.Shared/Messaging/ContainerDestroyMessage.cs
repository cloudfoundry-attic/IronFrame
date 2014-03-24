using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Shared.Messaging
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
        public ContainerDestroyResponse(string id, bool result = true) : base(id, result)
        {
        }
    }
}
