using System;
using IronFoundry.Container.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFoundry.Container.Messages
{
    public sealed class WaitForProcessExitParams
    {
        public Guid key;
        public int timeout;
    }

    public sealed class WaitForProcessExitRequest : JsonRpcRequest<WaitForProcessExitParams>
    {
        public static string MethodName = "Container.WaitForProcessExit";

        public WaitForProcessExitRequest(WaitForProcessExitParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    public sealed class WaitForProcessExitResult
    {
        public bool exited;
        public int exitCode;
    }

    public sealed class WaitForProcessExitResponse : JsonRpcResponse<WaitForProcessExitResult>
    {
        public WaitForProcessExitResponse(JToken id, WaitForProcessExitResult result)
            : base(id, result)
        {
        }
    }
}
