using System.Collections.Generic;
using System.IO;
using IronFoundry.Container;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class BindMountsParameters
    {
        public List<BindMount> Mounts { get; set; }
    }

    public class BindMountsRequest : JsonRpcRequest<BindMountsParameters>
    {
        public const string MethodName = "Container.BindMounts";

        public BindMountsRequest(BindMountsParameters parameters) : base(MethodName, parameters)
        {
        }
    }
    
    public class BindMountsResponse : JsonRpcResponse
    {
        public BindMountsResponse(JToken id) : base(id)
        {
        }
    }
}
