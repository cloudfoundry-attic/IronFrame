using IronFoundry.Warden.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;

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
        public ContainerStatisticsResponse(string id, ProcessStats stats) : base(id, stats)
        {
        }
    }
    
}
