using IronFoundry.Warden.Shared.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronFoundry.Warden.Shared.Messaging
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
