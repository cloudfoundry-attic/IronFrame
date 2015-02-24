using System;
using System.Collections.Generic;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    public sealed class CreateProcessParams
    {
        public Guid key;
        public string executablePath;
        public string[] arguments;
        public Dictionary<string, string> environment;
        public string workingDirectory;
    }

    public sealed class CreateProcessRequest : JsonRpcRequest<CreateProcessParams>
    {
        public static string MethodName = "Container.CreateProcess";

        public CreateProcessRequest(CreateProcessParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    public sealed class CreateProcessResult
    {
        public int id;
    }

    public sealed class CreateProcessResponse : JsonRpcResponse<CreateProcessResult>
    {
        public CreateProcessResponse(JToken id, CreateProcessResult result)
            : base(id, result)
        {
        }
    }
}
