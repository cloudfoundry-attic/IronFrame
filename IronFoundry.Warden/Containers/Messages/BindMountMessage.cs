using System.Collections.Generic;
using System.IO;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class BindMount
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public FileAccess Access { get; set; }
    }

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
