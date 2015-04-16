using System;
using System.Collections.Generic;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    internal class CreateProcessParams
    {
        public Guid key;
        public string executablePath;
        public string[] arguments;
        public Dictionary<string, string> environment;
        public string workingDirectory;
    }

    internal class CreateProcessRequest : JsonRpcRequest<CreateProcessParams>
    {
        public static string MethodName = "Container.CreateProcess";

        public CreateProcessRequest(CreateProcessParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    internal class CreateProcessResult
    {
        public int id;
    }

    internal class CreateProcessResponse : JsonRpcResponse<CreateProcessResult>
    {
        public CreateProcessResponse(JToken id, CreateProcessResult result)
            : base(id, result)
        {
        }
    }
}
