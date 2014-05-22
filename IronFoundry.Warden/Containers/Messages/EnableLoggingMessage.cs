using System.Collections.Generic;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class InstanceLoggingInfo
    {
        public InstanceLoggingInfo()
        {
            DrainUris = new List<string>();
        }

        public string ApplicationId { get; set; }
        public string InstanceIndex { get; set; }
        public string LoggregatorAddress { get; set; }
        public string LoggregatorSecret { get; set; }
        public List<string> DrainUris { get; private set; }

    }

    public class EnableLoggingRequest : JsonRpcRequest<InstanceLoggingInfo>
    {
        public static string MethodName = "Container.EnableLogging";

        public EnableLoggingRequest()
            : base(MethodName)
        {
        }
    }

    public class EnableLoggingResponse : JsonRpcResponse<bool>
    {
        public EnableLoggingResponse(JToken id)
            : base(id, true)
        {

        }
    }
}
