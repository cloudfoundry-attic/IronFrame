using System;
using IronFrame.Messaging;
using Newtonsoft.Json.Linq;

namespace IronFrame.Messages
{
    internal class WaitForProcessExitParams
    {
        public Guid key;
        public int timeout;
    }

    internal class WaitForProcessExitRequest : JsonRpcRequest<WaitForProcessExitParams>
    {
        public static string MethodName = "Container.WaitForProcessExit";

        public WaitForProcessExitRequest(WaitForProcessExitParams @params)
            : base(MethodName)
        {
            this.@params = @params;
        }
    }

    internal class WaitForProcessExitResult
    {
        public bool exited;
        public int exitCode;
    }

    internal class WaitForProcessExitResponse : JsonRpcResponse<WaitForProcessExitResult>
    {
        public WaitForProcessExitResponse(JToken id, WaitForProcessExitResult result)
            : base(id, result)
        {
        }
    }
}
