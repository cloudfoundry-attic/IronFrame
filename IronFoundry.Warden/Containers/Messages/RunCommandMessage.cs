using IronFoundry.Warden.Shared.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Warden.Containers.Messages
{
    public class RunCommandData
    {
        public bool privileged;
        public string command;
        public string[] arguments;
    }

    public class RunCommandRequest : JsonRpcRequest<RunCommandData>
    {
        public static string MethodName = "Container.RunCommand";
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
        public RunCommandResponse(JToken id, RunCommandResponseData result)
            : base(id, result)
        {
        }
    }
}
