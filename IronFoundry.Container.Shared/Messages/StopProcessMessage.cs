using System;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    public sealed class StopProcessParams
    {
        public Guid key;
        public int timeout;
    }

    public sealed class StopProcessRequest : JsonRpcRequest<StopProcessParams>
    {
        public static string MethodName = "Container.StopProcess";

        public StopProcessRequest(StopProcessParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    public sealed class StopProcessResponse : JsonRpcResponse
    {
        public StopProcessResponse(JToken id)
            : base(id)
        {
        }
    }
}