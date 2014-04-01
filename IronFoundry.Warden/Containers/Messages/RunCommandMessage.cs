using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronFoundry.Warden.Shared.Messaging;

namespace IronFoundry.Warden.Containers.Messages
{
    public class RunCommandData
    {
        public bool impersonate;
        public string command;
        public string[] arguments;
    }

    public class RunCommandRequest : JsonRpcRequest<RunCommandData>
    {
        public static string MethodName = "RunCommand";
        public RunCommandRequest(RunCommandData command)
            : base(MethodName)
        {
            this.@params = command;
        }
    }

    public class RunCommandResponseData
    {
        public int exitCode;
        public string stdErr;
        public string stdOut;
    }

    public class RunCommandResponse : JsonRpcResponse<RunCommandResponseData>
    {
        public RunCommandResponse(string id, RunCommandResponseData result)
            : base(id, result)
        {
        }
    }
}
