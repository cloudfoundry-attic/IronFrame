using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    internal class StopAllProcessesParams
    {
        public int timeout;
    }

    internal class StopAllProcessesRequest : JsonRpcRequest<StopAllProcessesParams>
    {
        public static string MethodName = "Container.StopAllProcesses";

        public StopAllProcessesRequest(StopAllProcessesParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    internal class StopAllProcessesResponse : JsonRpcResponse
    {
        public StopAllProcessesResponse(JToken id)
            : base(id)
        {
        }
    }
}
