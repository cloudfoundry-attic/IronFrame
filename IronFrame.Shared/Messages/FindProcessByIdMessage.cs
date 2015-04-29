using System;
using System.Collections.Generic;
using IronFrame.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFrame.Messages
{
    internal class FindProcessByIdParams
    {
        public int id;
    }

    internal class FindProcessByIdRequest : JsonRpcRequest<FindProcessByIdParams>
    {
        public static string MethodName = "Container.FindProcessById";

        public FindProcessByIdRequest(FindProcessByIdParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    internal class FindProcessByIdResult
    {
        public Guid processKey;
        public int id;
        public IReadOnlyDictionary<string,string> environment;
    }

    internal class FindProcessByIdResponse : JsonRpcResponse<FindProcessByIdResult>
    {
        public FindProcessByIdResponse(JToken id, FindProcessByIdResult result)
            : base(id, result)
        {
        }
    }
}
