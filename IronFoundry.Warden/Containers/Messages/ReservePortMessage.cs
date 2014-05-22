using IronFoundry.Warden.Shared.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ReservePortRequest : JsonRpcRequest<int>
    {
        public const string MethodName = "Container.ReservePort";

        public ReservePortRequest() : base(MethodName)
        {
        }

        public ReservePortRequest(int port)
            : base(MethodName, port)
        {
        }
    }

    public class ReservePortResponse : JsonRpcResponse<int>
    {
        public ReservePortResponse(Newtonsoft.Json.Linq.JToken id, int port) : base(id, port)
        {
        }
    }
}
