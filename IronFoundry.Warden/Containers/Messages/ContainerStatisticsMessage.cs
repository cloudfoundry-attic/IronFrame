using IronFoundry.Warden.Shared.Data;
using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class ContainerStatisticsRequest : JsonRpcRequest
    {
        public static readonly string MethodName = "GetContainerStatistics";

        public ContainerStatisticsRequest() : base(MethodName)
        {
        }
    }

    public class ContainerStatisticsResponse : JsonRpcResponse<ProcessStats>
    {
        public ContainerStatisticsResponse(JToken id, ProcessStats stats)
            : base(id, stats)
        {
        }
    }
    
}
