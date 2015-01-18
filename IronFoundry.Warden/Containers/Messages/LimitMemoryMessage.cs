using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class LimitMemoryInfo
    {
        public ulong LimitInBytes { get; set; }

        public LimitMemoryInfo()
        {
        }

        public LimitMemoryInfo(ulong limitInBytes)
        {
            LimitInBytes = limitInBytes;
        }
    }

    public class LimitMemoryRequest : JsonRpcRequest<LimitMemoryInfo>
    {
        public static readonly string MethodName = "Container.LimitMemory";

        public LimitMemoryRequest(LimitMemoryInfo info) : base(MethodName, info)
        {
        }
    }

    public class LimitMemoryResponse : JsonRpcResponse
    {
        public LimitMemoryResponse(JToken id) : base(id)
        {
        }
    }
}
