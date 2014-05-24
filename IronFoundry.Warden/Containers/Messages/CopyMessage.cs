using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class CopyInfo
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public CopyInfo()
        {
        }

        public CopyInfo(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }
    }

    public class CopyRequest : JsonRpcRequest<CopyInfo>
    {
        public static readonly string MethodName = "Container.Copy";

        public CopyRequest(CopyInfo info)
            : base(MethodName, info)
        {
        }
    }

    public class CopyResponse : JsonRpcResponse
    {
        public CopyResponse(JToken id)
            : base(id)
        {
        }
    }
}
